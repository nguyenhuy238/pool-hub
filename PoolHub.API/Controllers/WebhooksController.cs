using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PoolHub.Core.DTOs.Invoice;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PoolHub.API.Controllers;

public class VietQRData
{
    public long? Id { get; set; }
    public long? OrderCode { get; set; }
    public string? Tid { get; set; }
    public string? Description { get; set; }
    public string? Content { get; set; }
    public decimal Amount { get; set; }
    public decimal? TransferAmount { get; set; }
}

public class BankWebhookRequest
{
    public long? Id { get; set; }
    public string? Gateway { get; set; }
    public string? TransactionDate { get; set; }
    public string? AccountNumber { get; set; }
    public string? TransferType { get; set; }
    public decimal TransferAmount { get; set; }
    public decimal? Amount { get; set; }
    public string? Content { get; set; }
    public string? Description { get; set; }
    public string? ReferenceCode { get; set; }
    public VietQRData? Data { get; set; }
}

[ApiController]
[Route("api/webhooks")]
[AllowAnonymous]
public class WebhooksController(PoolHubDbContext db, IInvoiceService invoiceService, IConfiguration configuration) : ControllerBase
{
    [HttpPost("bank-transfer")]
    [HttpPost("sepay")]
    [HttpPost("casso")]
    [HttpPost("vietqr")]
    [HttpPost("payos")]
    public async Task<ActionResult<ApiResponse<object>>> HandleBankWebhook([FromBody] BankWebhookRequest request, CancellationToken ct)
    {
        // 1. Optional Secret Verification
        var webhookSecret = configuration["WebhookSettings:Secret"];
        if (!string.IsNullOrEmpty(webhookSecret))
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            var customHeader = Request.Headers["X-Webhook-Secret"].ToString();
            if (!string.Equals(authHeader, $"Apikey {webhookSecret}", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(authHeader, $"Bearer {webhookSecret}", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(customHeader, webhookSecret, StringComparison.Ordinal))
            {
                return Unauthorized(ApiResponse<object>.Fail("Invalid webhook secret token."));
            }
        }

        // 2. Ignore Outgoing transfers
        if (!string.IsNullOrEmpty(request.TransferType) && string.Equals(request.TransferType, "out", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(ApiResponse<object>.Ok(new { ignored = true }, "Outgoing transaction ignored."));
        }

        // 3. Extract Invoice Code (e.g. HD15) or OrderCode from PayOS / VietQR
        long invoiceId = 0;
        if (request.Data?.OrderCode > 0)
        {
            invoiceId = request.Data.OrderCode.Value;
        }
        else
        {
            var desc = request.Data?.Description ?? request.Data?.Content ?? request.Description ?? request.Content ?? "";
            var rawText = desc.ToUpperInvariant();
            var match = Regex.Match(rawText, @"HD\s*(\d+)");
            if (match.Success)
            {
                long.TryParse(match.Groups[1].Value, out invoiceId);
            }
        }

        if (invoiceId <= 0)
        {
            return Ok(ApiResponse<object>.Ok(new { handled = false }, "No valid invoice code (HDxxx) or OrderCode found in transaction content."));
        }

        // 4. Find Invoice
        var invoice = await db.Invoices.FirstOrDefaultAsync(x => x.InvoiceId == invoiceId, ct);
        if (invoice == null)
        {
            return Ok(ApiResponse<object>.Ok(new { handled = false }, $"Invoice HD{invoiceId} does not exist."));
        }

        if (invoice.Status == 3 || invoice.PaymentStatus == 2) // Cancelled or Paid
        {
            return Ok(ApiResponse<object>.Ok(new { handled = true }, $"Invoice HD{invoiceId} is already paid or cancelled."));
        }

        // 5. Determine amount to pay
        var remainingAmount = invoice.GrandTotalAmount - invoice.PaidAmount;
        if (remainingAmount <= 0)
        {
            return Ok(ApiResponse<object>.Ok(new { handled = true }, $"Invoice HD{invoiceId} already fully paid."));
        }

        var incomingAmount = request.Data?.Amount > 0 ? request.Data.Amount :
                             (request.Data?.TransferAmount > 0 ? request.Data.TransferAmount.Value :
                             (request.TransferAmount > 0 ? request.TransferAmount : (request.Amount ?? 0)));

        var payAmount = Math.Min(incomingAmount, remainingAmount);
        if (payAmount <= 0)
        {
            return Ok(ApiResponse<object>.Ok(new { handled = false }, "Transfer amount is 0 or invalid."));
        }

        // 6. Get Bank payment method
        var bankMethod = await db.PaymentMethods.FirstOrDefaultAsync(x => x.Code == "BANK" || x.Name.Contains("Chuyển") || x.Name.Contains("QR") || x.Name.Contains("Bank"), ct);
        var methodId = bankMethod?.PaymentMethodId ?? 1;

        // 7. Execute automated payment
        await invoiceService.CreatePaymentAsync(new CreatePaymentRequest
        {
            InvoiceId = invoiceId,
            PaymentMethodId = methodId,
            Amount = payAmount
        }, null, ct);

        return Ok(ApiResponse<object>.Ok(new { success = true, invoiceId, paidAmount = payAmount }, $"Automated payment of {payAmount} applied to Invoice HD{invoiceId}."));
    }
}
