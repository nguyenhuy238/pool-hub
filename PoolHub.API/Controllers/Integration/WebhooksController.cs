using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PoolHub.Core.DTOs.Invoice;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
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
public class WebhooksController(PoolHubDbContext db, IInvoiceService invoiceService, IBookingService bookingService, IConfiguration configuration) : ControllerBase
{
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

        var desc = request.Data?.Description ?? request.Data?.Content ?? request.Description ?? request.Content ?? "";
        var rawText = desc.ToUpperInvariant();

        // 3. Check for Booking Deposit (OrderCode >= 100,000,000 or content contains BKxxx / DEPxxx)
        long depositId = 0;
        if (request.Data?.OrderCode >= 100000000 && request.Data?.OrderCode < 200000000)
        {
            depositId = request.Data.OrderCode.Value - 100000000;
        }
        else if (request.Data?.OrderCode >= 200000000)
        {
            var foundByNote = await (from b in db.Bookings
                                     join d in db.BookingDeposits on b.BookingId equals d.BookingId
                                     where b.Note != null && b.Note.Contains("PayOS_OrderCode:" + request.Data.OrderCode.Value)
                                     select d).FirstOrDefaultAsync(ct);
            if (foundByNote != null) depositId = foundByNote.BookingDepositId;
        }

        if (depositId == 0)
        {
            var depMatch = Regex.Match(rawText, @"DEP\s*(\d+)");
            if (depMatch.Success && long.TryParse(depMatch.Groups[1].Value, out var parsedDepId))
            {
                depositId = parsedDepId;
            }
            else
            {
                var bkMatch = Regex.Match(rawText, @"BK\s*([A-Z0-9]+)");
                if (bkMatch.Success)
                {
                    var bkCode = "BK" + bkMatch.Groups[1].Value;
                    var foundDeposit = await (from d in db.BookingDeposits
                                              join b in db.Bookings on d.BookingId equals b.BookingId
                                              where b.BookingCode.Contains(bkCode) || bkCode.Contains(b.BookingCode)
                                              select d).FirstOrDefaultAsync(ct);
                    if (foundDeposit != null) depositId = foundDeposit.BookingDepositId;
                }
            }
        }

        if (depositId > 0)
        {
            var deposit = await db.BookingDeposits.FirstOrDefaultAsync(x => x.BookingDepositId == depositId, ct);
            if (deposit != null)
            {
                if (deposit.Status == PoolHub.Shared.Constants.BookingDepositStatuses.Paid)
                {
                    return Ok(ApiResponse<object>.Ok(new { handled = true }, $"Booking deposit #{depositId} is already paid."));
                }

                var incomingDepositAmount = request.Data?.Amount > 0 ? request.Data.Amount :
                                           (request.Data?.TransferAmount > 0 ? request.Data.TransferAmount.Value :
                                           (request.TransferAmount > 0 ? request.TransferAmount : (request.Amount ?? 0)));

                if (incomingDepositAmount < deposit.RequiredAmount)
                {
                    return Ok(ApiResponse<object>.Ok(new { handled = false }, $"Transfer amount {incomingDepositAmount} is less than required deposit {deposit.RequiredAmount}."));
                }

                await bookingService.ConfirmDepositAsync(deposit.BookingId, new PoolHub.Core.DTOs.Booking.ConfirmDepositRequest
                {
                    PaidAmount = incomingDepositAmount,
                    TransactionCode = request.ReferenceCode ?? request.Data?.Tid ?? $"PAYOS_{depositId}"
                }, 1, ct);

                return Ok(ApiResponse<object>.Ok(new { success = true, depositId, bookingId = deposit.BookingId, paidAmount = incomingDepositAmount }, $"Automated deposit payment of {incomingDepositAmount} applied to Booking #{deposit.BookingId}."));
            }
            else if (request.Data?.OrderCode < 100000000)
            {
                return Ok(ApiResponse<object>.Ok(new { handled = false }, $"Booking deposit #{depositId} does not exist."));
            }
        }

        // 4. Extract Invoice Code (e.g. HD15) or OrderCode (< 100,000,000 or retry OrderCode >= 100,000,000) from PayOS / VietQR
        long invoiceId = 0;
        if (request.Data?.OrderCode > 0 && request.Data.OrderCode < 100000000)
        {
            invoiceId = request.Data.OrderCode.Value;
        }
        else if (request.Data?.OrderCode >= 100000000)
        {
            var foundInv = await db.Invoices.FirstOrDefaultAsync(x => x.Note != null && x.Note.Contains($"PayOS_OrderCode:{request.Data.OrderCode.Value}"), ct);
            if (foundInv != null)
            {
                invoiceId = foundInv.InvoiceId;
            }
        }

        if (invoiceId <= 0)
        {
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

        if (invoice.Status == 3 || invoice.PaymentStatus == InvoicePaymentStatuses.Paid || invoice.PaidAmount >= invoice.GrandTotalAmount) // Cancelled or Paid
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
