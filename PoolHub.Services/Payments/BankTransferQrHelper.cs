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
            BankName = ResolveBankName(bankCode, bankName),
            BankCode = bankCode,
            AccountNumber = accountNumber,
            AccountName = accountName,
            QrImageUrl = qrImageUrl
        };
    }

    public static string ResolveBankName(string? bankCode, string? bankName = null)
    {
        if (!string.IsNullOrWhiteSpace(bankName) && !bankName.All(char.IsDigit))
        {
            return bankName;
        }

        var code = (bankCode ?? bankName ?? string.Empty).Trim().ToUpperInvariant();
        return code switch
        {
            "970422" or "MB" or "MBBANK" => "MBBank (Ngân hàng TMCP Quân Đội)",
            "970436" or "VCB" or "VIETCOMBANK" => "Vietcombank (Ngân hàng TMCP Ngoại Thương Việt Nam)",
            "970415" or "ICB" or "VIETINBANK" => "VietinBank (Ngân hàng TMCP Công Thương Việt Nam)",
            "970418" or "BIDV" => "BIDV (Ngân hàng TMCP Đầu tư và Phát triển Việt Nam)",
            "970405" or "VBA" or "AGRIBANK" => "Agribank (Ngân hàng Nông nghiệp và Phát triển Nông thôn)",
            "970423" or "TPB" or "TPBANK" => "TPBank (Ngân hàng TMCP Tiên Phong)",
            "970432" or "VPB" or "VPBANK" => "VPBank (Ngân hàng TMCP Việt Nam Thịnh Vượng)",
            "970403" or "STB" or "SACOMBANK" => "Sacombank (Ngân hàng TMCP Sài Gòn Thương Tín)",
            "970441" or "VIB" => "VIB (Ngân hàng TMCP Quốc tế Việt Nam)",
            "970443" or "SHB" => "SHB (Ngân hàng TMCP Sài Gòn - Hà Nội)",
            "970431" or "EIB" or "EXIMBANK" => "Eximbank (Ngân hàng TMCP Xuất Nhập Khẩu Việt Nam)",
            "970454" or "BVB" or "BVBANK" => "BVBank (Ngân hàng TMCP Bản Việt)",
            "970407" or "TCB" or "TECHCOMBANK" => "Techcombank (Ngân hàng TMCP Kỹ Thương Việt Nam)",
            "970426" or "MSB" => "MSB (Ngân hàng TMCP Hàng Hải Việt Nam)",
            "970416" or "ACB" => "ACB (Ngân hàng TMCP Á Châu)",
            "970448" or "OCB" => "OCB (Ngân hàng TMCP Phương Đông)",
            "970428" or "NAB" or "NAMABANK" => "Nam A Bank (Ngân hàng TMCP Nam Á)",
            "970460" or "KLB" => "Kienlongbank (Ngân hàng TMCP Kiên Long)",
            "970437" or "HDB" or "HDBANK" => "HDBank (Ngân hàng TMCP Phát triển TP. HCM)",
            "970440" or "SEAB" or "SEABANK" => "SeABank (Ngân hàng TMCP Đông Nam Á)",
            "970412" or "PVCOMBANK" => "PVcomBank (Ngân hàng TMCP Đại Chúng Việt Nam)",
            _ => !string.IsNullOrWhiteSpace(code) ? code : "Ngân hàng chuyển khoản"
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
