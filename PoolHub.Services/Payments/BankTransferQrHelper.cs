using System.Text.Json;
using PoolHub.Core.Entities;

namespace PoolHub.Services.Payments;

internal sealed class BankTransferQrConfig
{
    public long PaymentMethodId { get; init; }
    public string PaymentMethodCode { get; init; } = string.Empty;
    public string PaymentMethodName { get; init; } = string.Empty;
    public string BankName { get; init; } = string.Empty;
    public string BankCode { get; init; } = string.Empty;
    public string AccountNumber { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public string? QrImageUrl { get; init; }

    public bool CanBuildDynamicQr =>
        !string.IsNullOrWhiteSpace(BankCode) &&
        !string.IsNullOrWhiteSpace(AccountNumber) &&
        !string.IsNullOrWhiteSpace(AccountName);
}

internal static class BankTransferQrHelper
{
    public static bool IsBankTransferMethod(PaymentMethod method)
    {
        var code = method.Code.Trim().ToUpperInvariant();
        var name = method.Name.Trim();
        return method.IsActive &&
               code != "DEPOSIT" &&
               (code is "BANK" or "BANK_TRANSFER" or "TRANSFER" or "VIETQR" ||
                name.Contains("Chuyển", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Bank", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("QR", StringComparison.OrdinalIgnoreCase));
    }

    public static BankTransferQrConfig? Parse(PaymentMethod? method)
    {
        if (method is null || !IsBankTransferMethod(method)) return null;

        var bankName = string.Empty;
        var bankCode = string.Empty;
        var accountNumber = string.Empty;
        var accountName = string.Empty;
        string? qrImageUrl = null;

        if (!string.IsNullOrWhiteSpace(method.Description) && method.Description.TrimStart().StartsWith('{'))
        {
            try
            {
                using var doc = JsonDocument.Parse(method.Description);
                var root = doc.RootElement;
                bankName = GetString(root, "bankName");
                bankCode = GetString(root, "bankCode");
                accountNumber = GetString(root, "accountNo");
                if (string.IsNullOrWhiteSpace(accountNumber)) accountNumber = GetString(root, "accountNumber");
                accountName = GetString(root, "accountName");
                qrImageUrl = GetString(root, "qrImageUrl");
            }
            catch (JsonException)
            {
                return null;
            }
        }

        return new BankTransferQrConfig
        {
            PaymentMethodId = method.PaymentMethodId,
            PaymentMethodCode = method.Code,
            PaymentMethodName = method.Name,
            BankName = string.IsNullOrWhiteSpace(bankName) ? bankCode : bankName,
            BankCode = bankCode,
            AccountNumber = accountNumber,
            AccountName = accountName,
            QrImageUrl = qrImageUrl
        };
    }

    public static string BuildVietQrUrl(BankTransferQrConfig config, decimal amount, string transferContent)
    {
        var roundedAmount = Math.Max(0, (long)Math.Round(amount, 0, MidpointRounding.AwayFromZero));
        return $"https://img.vietqr.io/image/{config.BankCode}-{config.AccountNumber}-compact2.png?amount={roundedAmount}&addInfo={Uri.EscapeDataString(transferContent)}&accountName={Uri.EscapeDataString(config.AccountName)}";
    }

    private static string GetString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) ? value.GetString() ?? string.Empty : string.Empty;
}
