using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PoolHub.Core.DTOs.Invoice;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EntityInvoice = PoolHub.Core.Entities.Invoice;

namespace PoolHub.Services.Invoice;

public class InvoiceService(PoolHubDbContext db, IConfiguration? config = null, IHttpClientFactory? httpClientFactory = null) : IInvoiceService
{
    public async Task<PagedResult<InvoiceDto>> GetInvoicesAsync(InvoiceQueryRequest request, CancellationToken ct)
    {
        var query = db.Invoices.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(x => x.InvoiceCode.Contains(request.Search));
        }

        if (request.PaymentStatus.HasValue)
        {
            query = query.Where(x => x.PaymentStatus == request.PaymentStatus.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status.Value);
        }

        if (request.Date.HasValue)
        {
            query = query.Where(x => x.IssuedAtUtc.HasValue && x.IssuedAtUtc.Value.Date == request.Date.Value.Date);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.InvoiceId)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new InvoiceDto
            {
                InvoiceId = x.InvoiceId,
                SessionId = x.SessionId,
                InvoiceCode = x.InvoiceCode,
                GrandTotalAmount = x.GrandTotalAmount,
                PaymentStatus = x.PaymentStatus,
                Status = x.Status
            })
            .ToListAsync(ct);

        return new PagedResult<InvoiceDto>
        {
            Items = items,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }

    public async Task<InvoiceDto> GenerateFromSessionAsync(long sessionId, long? issuedByUserId, CancellationToken ct)
    {
        var exists = await db.Invoices.FirstOrDefaultAsync(x => x.SessionId == sessionId, ct);
        if (exists is not null) 
        {
            return new InvoiceDto { InvoiceId = exists.InvoiceId, SessionId = exists.SessionId, InvoiceCode = exists.InvoiceCode, GrandTotalAmount = exists.GrandTotalAmount };
        }

        var session = await db.Sessions.FindAsync([sessionId], ct) ?? throw new NotFoundException("Session not found.");
        
        // If session is still active, close it automatically so everything is calculated
        if (session.Status == 1)
        {
            await CloseSessionInternalAsync(sessionId, issuedByUserId, DateTime.UtcNow, ct);
            await db.SaveChangesAsync(ct);
        }

        // Sum amounts
        var productTotal = await db.Orders.Where(o => o.SessionId == sessionId && o.Status != 3).SumAsync(o => o.SubtotalAmount, ct);
        var timeTotal = await db.SessionTableAssignments.Where(x => x.SessionId == sessionId).SumAsync(x => x.Amount ?? 0, ct);
        var subtotal = productTotal + timeTotal;

        var invoice = new EntityInvoice
        {
            SessionId = sessionId,
            CustomerId = session.CustomerId,
            InvoiceCode = $"INV{DateTime.UtcNow:yyyyMMddHHmmss}",
            TimeSubtotalAmount = timeTotal,
            ProductSubtotalAmount = productTotal,
            SubtotalAmount = subtotal,
            DiscountAmount = 0,
            TaxAmount = 0,
            GrandTotalAmount = subtotal,
            PaidAmount = 0,
            PaymentStatus = 1,
            Status = 1,
            IssuedByUserId = issuedByUserId,
            IssuedAtUtc = DateTime.UtcNow
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct);

        // Generate InvoiceLines
        var assignments = await db.SessionTableAssignments.Where(x => x.SessionId == sessionId).ToListAsync(ct);
        foreach (var assignment in assignments)
        {
            var table = await db.VenueTables.FindAsync([assignment.TableId], ct);
            db.InvoiceLines.Add(new InvoiceLine
            {
                InvoiceId = invoice.InvoiceId,
                LineType = "TIME",
                ReferenceId = assignment.SessionTableAssignmentId,
                Description = $"Time played on table {(table != null ? table.TableName : assignment.TableId.ToString())}",
                Quantity = (decimal)(assignment.DurationMinutes ?? 0) / 60m,
                UnitPrice = assignment.HourlyRateSnapshot,
                LineTotalAmount = assignment.Amount ?? 0
            });
        }

        var orders = await db.Orders.Where(o => o.SessionId == sessionId && o.Status != 3).ToListAsync(ct);
        foreach (var order in orders)
        {
            var items = await db.OrderItems.Where(oi => oi.OrderId == order.OrderId).ToListAsync(ct);
            foreach (var item in items)
            {
                db.InvoiceLines.Add(new InvoiceLine
                {
                    InvoiceId = invoice.InvoiceId,
                    LineType = "PRODUCT",
                    ReferenceId = item.OrderItemId,
                    Description = item.ProductNameSnapshot,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPriceSnapshot,
                    LineTotalAmount = item.LineTotalAmount
                });
            }
        }

        await db.SaveChangesAsync(ct);
        return new InvoiceDto { InvoiceId = invoice.InvoiceId, SessionId = invoice.SessionId, InvoiceCode = invoice.InvoiceCode, GrandTotalAmount = invoice.GrandTotalAmount };
    }

    public async Task CreatePaymentAsync(CreatePaymentRequest request, long? receivedByUserId, CancellationToken ct)
    {
        if (request.Amount <= 0)
        {
            throw new ValidationException("Payment amount must be greater than zero.");
        }

        var invoice = await db.Invoices.FindAsync([request.InvoiceId], ct) ?? throw new NotFoundException("Invoice not found.");
        if (invoice.Status == 3)
        {
            throw new BusinessRuleException("Cannot pay a cancelled invoice.");
        }

        if (invoice.PaymentStatus == InvoicePaymentStatuses.Paid || invoice.PaidAmount >= invoice.GrandTotalAmount)
        {
            throw new BusinessRuleException("Invoice is already fully paid.");
        }

        if (!await db.PaymentMethods.AnyAsync(x => x.PaymentMethodId == request.PaymentMethodId && x.IsActive, ct))
        {
            throw new ValidationException("Payment method is invalid or inactive.");
        }

        if (invoice.PaidAmount + request.Amount > invoice.GrandTotalAmount)
        {
            throw new BusinessRuleException("Total payment amount cannot exceed the grand total amount.");
        }

        var payment = new Payment 
        { 
            InvoiceId = request.InvoiceId, 
            PaymentMethodId = request.PaymentMethodId, 
            Amount = request.Amount, 
            PaymentStatus = PaymentStatuses.Completed,
            TransactionCode = $"TXN{DateTime.UtcNow:HHmmssddMMyyyy}",
            ReceivedByUserId = receivedByUserId, 
            PaidAtUtc = DateTime.UtcNow 
        };
        db.Payments.Add(payment);

        invoice.PaidAmount += request.Amount;
        if (invoice.PaidAmount >= invoice.GrandTotalAmount)
        {
            invoice.PaymentStatus = InvoicePaymentStatuses.Paid;
            invoice.Status = 2; // Completed
        }

        await db.SaveChangesAsync(ct);
    }

    public Task<List<PaymentMethodDto>> GetPaymentMethodsAsync(CancellationToken ct)
        => db.PaymentMethods
            .Where(x => x.IsActive)
            .OrderBy(x => x.PaymentMethodId)
            .Select(x => new PaymentMethodDto
            {
                PaymentMethodId = x.PaymentMethodId,
                Name = x.Name,
                Code = x.Code,
                Description = x.Description,
                IsActive = x.IsActive
            })
            .ToListAsync(ct);

    public async Task<InvoiceDetailDto> GetInvoiceDetailAsync(long id, CancellationToken ct)
    {
        var invoice = await db.Invoices.FindAsync([id], ct) ?? throw new NotFoundException("Invoice not found.");
        
        var lines = await db.InvoiceLines
            .Where(x => x.InvoiceId == id)
            .Select(x => new InvoiceLineDto
            {
                InvoiceLineId = x.InvoiceLineId,
                InvoiceId = x.InvoiceId,
                LineType = x.LineType,
                ReferenceId = x.ReferenceId,
                Description = x.Description,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                LineTotalAmount = x.LineTotalAmount
            })
            .ToListAsync(ct);

        var discounts = await db.InvoiceDiscounts
            .Where(x => x.InvoiceId == id)
            .Select(x => new InvoiceDiscountDto
            {
                InvoiceDiscountId = x.InvoiceDiscountId,
                InvoiceId = x.InvoiceId,
                DiscountId = x.DiscountId,
                AppliedByUserId = x.AppliedByUserId,
                AmountApplied = x.AmountApplied,
                DescriptionSnapshot = x.DescriptionSnapshot
            })
            .ToListAsync(ct);

        var payments = await (from p in db.Payments
                              where p.InvoiceId == id
                              join pm in db.PaymentMethods.AsNoTracking() on p.PaymentMethodId equals pm.PaymentMethodId
                              select new PaymentDto
                              {
                                  PaymentId = p.PaymentId,
                                  InvoiceId = p.InvoiceId,
                                  InvoiceCode = invoice.InvoiceCode,
                                  PaymentMethodId = p.PaymentMethodId,
                                  PaymentMethodName = pm.Name,
                                  Amount = p.Amount,
                                  PaymentStatus = p.PaymentStatus,
                                  TransactionCode = p.TransactionCode,
                                  PaidAtUtc = p.PaidAtUtc,
                                  ReceivedByUserId = p.ReceivedByUserId,
                                  Note = p.Note
                              }).ToListAsync(ct);

        return new InvoiceDetailDto
        {
            InvoiceId = invoice.InvoiceId,
            InvoiceCode = invoice.InvoiceCode,
            SessionId = invoice.SessionId,
            CustomerId = invoice.CustomerId,
            TimeSubtotalAmount = invoice.TimeSubtotalAmount,
            ProductSubtotalAmount = invoice.ProductSubtotalAmount,
            SubtotalAmount = invoice.SubtotalAmount,
            DiscountAmount = invoice.DiscountAmount,
            TaxAmount = invoice.TaxAmount,
            GrandTotalAmount = invoice.GrandTotalAmount,
            PaidAmount = invoice.PaidAmount,
            PaymentStatus = invoice.PaymentStatus,
            Status = invoice.Status,
            IssuedByUserId = invoice.IssuedByUserId,
            IssuedAtUtc = invoice.IssuedAtUtc,
            Note = invoice.Note,
            Lines = lines,
            Discounts = discounts,
            Payments = payments
        };
    }

    public async Task ApplyDiscountAsync(long invoiceId, ApplyDiscountRequest request, long userId, CancellationToken ct)
    {
        var invoice = await db.Invoices.FindAsync([invoiceId], ct) ?? throw new NotFoundException("Invoice not found.");
        if (invoice.Status == 3)
        {
            throw new BusinessRuleException("Cannot apply discounts to a cancelled invoice.");
        }
        if (invoice.PaymentStatus == InvoicePaymentStatuses.Paid)
        {
            throw new BusinessRuleException("Cannot apply discounts to a fully paid invoice.");
        }

        var code = request.DiscountCode.Trim().ToUpper();
        var now = DateTime.UtcNow;
        var discount = await db.Discounts
            .FirstOrDefaultAsync(d => d.DiscountCode.ToUpper() == code && d.IsActive && d.StartsAtUtc <= now && (d.EndsAtUtc == null || d.EndsAtUtc >= now), ct)
            ?? throw new NotFoundException("Mã giảm giá không tồn tại, đã hết hạn hoặc chưa kích hoạt.");

        if (discount.MinTimeSubtotal.HasValue && invoice.TimeSubtotalAmount < discount.MinTimeSubtotal.Value)
        {
            throw new BusinessRuleException($"Hóa đơn cần đạt tối thiểu {discount.MinTimeSubtotal.Value:N0} VND tiền giờ chơi để áp dụng mã này.");
        }

        var existingDiscounts = await db.InvoiceDiscounts
            .Where(id => id.InvoiceId == invoiceId)
            .ToListAsync(ct);
        if (existingDiscounts.Count > 0)
        {
            db.InvoiceDiscounts.RemoveRange(existingDiscounts);
        }

        decimal baseAmount = invoice.SubtotalAmount;

        decimal discountAmt = 0;
        if (discount.DiscountType.Equals(DiscountTypes.Percentage, StringComparison.OrdinalIgnoreCase))
        {
            discountAmt = baseAmount * (discount.Value / 100m);
        }
        else if (discount.DiscountType.Equals(DiscountTypes.FixedAmount, StringComparison.OrdinalIgnoreCase)
                 || discount.DiscountType.Equals("FIXED", StringComparison.OrdinalIgnoreCase))
        {
            discountAmt = discount.Value;
        }

        if (discount.MaxAmount.HasValue && discountAmt > discount.MaxAmount.Value)
        {
            discountAmt = discount.MaxAmount.Value;
        }

        if (discountAmt > baseAmount)
        {
            discountAmt = baseAmount;
        }

        var invoiceDiscount = new InvoiceDiscount
        {
            InvoiceId = invoiceId,
            DiscountId = discount.DiscountId,
            AppliedByUserId = userId,
            AmountApplied = discountAmt,
            DescriptionSnapshot = $"{discount.Name} ({discount.DiscountCode})"
        };
        db.InvoiceDiscounts.Add(invoiceDiscount);

        invoice.DiscountAmount = discountAmt;
        invoice.GrandTotalAmount = Math.Max(0, invoice.SubtotalAmount - invoice.DiscountAmount + invoice.TaxAmount);

        if (invoice.PaidAmount >= invoice.GrandTotalAmount)
        {
            invoice.PaymentStatus = InvoicePaymentStatuses.Paid;
            invoice.Status = 2; // Completed
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveDiscountAsync(long invoiceId, long userId, CancellationToken ct)
    {
        var invoice = await db.Invoices.FindAsync([invoiceId], ct) ?? throw new NotFoundException("Invoice not found.");
        if (invoice.Status == 3) throw new BusinessRuleException("Cannot modify a cancelled invoice.");
        if (invoice.PaymentStatus == InvoicePaymentStatuses.Paid) throw new BusinessRuleException("Cannot modify a fully paid invoice.");

        var existingDiscounts = await db.InvoiceDiscounts.Where(id => id.InvoiceId == invoiceId).ToListAsync(ct);
        if (existingDiscounts.Count > 0)
        {
            db.InvoiceDiscounts.RemoveRange(existingDiscounts);
        }

        invoice.DiscountAmount = 0;
        invoice.GrandTotalAmount = Math.Max(0, invoice.SubtotalAmount + invoice.TaxAmount);

        if (invoice.PaidAmount >= invoice.GrandTotalAmount && invoice.GrandTotalAmount > 0)
        {
            invoice.PaymentStatus = InvoicePaymentStatuses.Paid;
            invoice.Status = 2; // Completed
        }
        else if (invoice.PaidAmount > 0)
        {
            invoice.PaymentStatus = 1; // Partial
        }
        else
        {
            invoice.PaymentStatus = 0; // Unpaid
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task CloseSessionInternalAsync(long sessionId, long? closedByUserId, DateTime endUtc, CancellationToken ct)
    {
        var session = await db.Sessions.FindAsync([sessionId], ct);
        if (session == null || session.Status == 2) return;

        session.Status = 2; // Closed
        session.EndedAtUtc = endUtc;
        session.ClosedByUserId = closedByUserId;

        var activeAssignments = await db.SessionTableAssignments
            .Where(x => x.SessionId == sessionId && x.EndedAtUtc == null)
            .ToListAsync(ct);

        foreach (var assignment in activeAssignments)
        {
            assignment.EndedAtUtc = endUtc;
            var table = await db.VenueTables.FindAsync([assignment.TableId], ct);
            if (table != null)
            {
                table.OperationalStatus = 1; // Available
                var rule = await FindActiveRuleAsync(table.TableTypeId, assignment.StartedAtUtc, ct);
                var durationMinutes = (int)Math.Ceiling((endUtc - assignment.StartedAtUtc).TotalMinutes);
                if (durationMinutes < 0) durationMinutes = 0;
                assignment.DurationMinutes = durationMinutes;

                if (rule != null)
                {
                    assignment.PricingPlanRuleId = rule.PricingPlanRuleId;
                    assignment.HourlyRateSnapshot = rule.HourlyRate;

                    var billableMinutes = durationMinutes;
                    if (billableMinutes < rule.MinimumMinutes)
                    {
                        billableMinutes = rule.MinimumMinutes;
                    }
                    if (rule.BillingBlockMinutes > 0)
                    {
                        var remainder = billableMinutes % rule.BillingBlockMinutes;
                        if (remainder > 0)
                        {
                            billableMinutes += (rule.BillingBlockMinutes - remainder);
                        }
                    }
                    assignment.Amount = ((decimal)billableMinutes / 60m) * rule.HourlyRate;
                }
                else
                {
                    throw new ConflictException($"No active pricing rule found for table {table.TableCode} at assignment start time.");
                }
            }
        }
    }

    private async Task<PricingPlanRule?> FindActiveRuleAsync(long tableTypeId, DateTime time, CancellationToken ct)
    {
        int dayOfWeek = (int)time.DayOfWeek;
        TimeSpan timeOfDay = time.TimeOfDay;

        var activePlans = await db.PricingPlans
            .Where(p => p.IsActive && p.StartsAtUtc <= time && (p.EndsAtUtc == null || p.EndsAtUtc >= time))
            .ToListAsync(ct);

        if (!activePlans.Any()) return null;

        var planIds = activePlans.OrderByDescending(p => p.IsDefault).Select(p => p.PricingPlanId).ToList();

        foreach (var planId in planIds)
        {
            var rule = await db.PricingPlanRules
                .FirstOrDefaultAsync(r => r.PricingPlanId == planId && 
                                          r.TableTypeId == tableTypeId && 
                                          r.DayOfWeek == dayOfWeek && 
                                          r.StartTime <= timeOfDay && 
                                          r.EndTime >= timeOfDay && 
                                          r.IsActive, ct);
            if (rule != null) return rule;
        }

        var defaultPlan = activePlans.FirstOrDefault(p => p.IsDefault);
        if (defaultPlan != null)
        {
            var rule = await db.PricingPlanRules
                .FirstOrDefaultAsync(r => r.PricingPlanId == defaultPlan.PricingPlanId && 
                                          r.TableTypeId == tableTypeId && 
                                          r.DayOfWeek == dayOfWeek && 
                                          r.IsActive, ct);
            if (rule != null) return rule;
        }

        return null;
    }

    public async Task CancelInvoiceAsync(long invoiceId, string reason, long userId, CancellationToken ct)
    {
        var invoice = await db.Invoices.FindAsync([invoiceId], ct) ?? throw new NotFoundException("Invoice not found.");
        if (invoice.PaymentStatus == InvoicePaymentStatuses.Paid)
        {
            throw new BusinessRuleException("Cannot cancel a paid invoice.");
        }
        if (invoice.Status == 3) // 3 is usually Cancelled
        {
            throw new BusinessRuleException("Invoice is already cancelled.");
        }
        
        invoice.Status = 3; // Cancelled
        invoice.Note = string.IsNullOrWhiteSpace(invoice.Note) ? $"Cancelled: {reason}" : $"{invoice.Note} | Cancelled: {reason}";
        
        await db.SaveChangesAsync(ct);
    }

    public async Task<string> ExportPdfAsync(long invoiceId, CancellationToken ct)
    {
        _ = await db.Invoices.FindAsync([invoiceId], ct) ?? throw new NotFoundException("Invoice not found.");
        throw new BusinessRuleException("PDF export is not supported by the backend yet.");
    }

    public async Task RefundPaymentAsync(long paymentId, string reason, long userId, CancellationToken ct)
    {
        var payment = await db.Payments.FindAsync([paymentId], ct) ?? throw new NotFoundException("Payment not found.");
        if (payment.PaymentStatus == PaymentStatuses.Refunded) throw new BusinessRuleException("Payment is already refunded.");
        payment.PaymentStatus = PaymentStatuses.Refunded;
        payment.Note = string.IsNullOrWhiteSpace(payment.Note) ? $"Refunded: {reason}" : $"{payment.Note} | Refunded: {reason}";
        
        var invoice = await db.Invoices.FindAsync([payment.InvoiceId], ct);
        if (invoice != null)
        {
            invoice.PaidAmount = Math.Max(0, invoice.PaidAmount - payment.Amount);
            if (invoice.PaidAmount < invoice.GrandTotalAmount)
            {
                invoice.PaymentStatus = invoice.PaidAmount <= 0 ? InvoicePaymentStatuses.Unpaid : InvoicePaymentStatuses.PartiallyPaid;
            }
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task<string> GetVietQrUrlAsync(long invoiceId, CancellationToken ct)
    {
        var invoice = await db.Invoices.FindAsync([invoiceId], ct) ?? throw new NotFoundException("Invoice not found.");
        var bankMethod = await db.PaymentMethods.FirstOrDefaultAsync(x => x.Code == "BANK" || x.Name.Contains("Chuyển") || x.Name.Contains("QR") || x.Name.Contains("Bank"), ct);
        var desc = bankMethod?.Description ?? "";
        
        string bankCode = "MB";
        string accountNo = "989420048989";
        string accountName = "POOLHUB";

        try {
            using var doc = System.Text.Json.JsonDocument.Parse(desc);
            var root = doc.RootElement;
            if (root.TryGetProperty("bankCode", out var bc) && !string.IsNullOrEmpty(bc.GetString())) bankCode = bc.GetString()!;
            if (root.TryGetProperty("accountNo", out var ac) && !string.IsNullOrEmpty(ac.GetString())) accountNo = ac.GetString()!;
            if (root.TryGetProperty("accountName", out var an) && !string.IsNullOrEmpty(an.GetString())) accountName = an.GetString()!;
        } catch {}

        var amount = (long)(invoice.GrandTotalAmount - invoice.PaidAmount);
        if (amount <= 0) amount = (long)invoice.GrandTotalAmount;
        var addInfo = $"HD{invoiceId}";

        var clientId = config?["PayOSSettings:ClientId"];
        var apiKey = config?["PayOSSettings:ApiKey"];
        var checksumKey = config?["PayOSSettings:ChecksumKey"];
        var baseUrl = config?["EmailSettings:FrontendBaseUrl"] ?? "http://localhost:3000";

        if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(checksumKey) && httpClientFactory != null)
        {
            try
            {
                string cancelUrl = $"{baseUrl}/operation/invoices";
                string returnUrl = $"{baseUrl}/operation/invoices";
                string description = $"HD{invoiceId}";
                long orderCode = invoiceId;

                string rawData = $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}";
                using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(checksumKey));
                var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawData));
                var signature = BitConverter.ToString(hash).Replace("-", "").ToLower();

                var payosBody = new
                {
                    orderCode = orderCode,
                    amount = amount,
                    description = description,
                    cancelUrl = cancelUrl,
                    returnUrl = returnUrl,
                    signature = signature
                };

                var client = httpClientFactory.CreateClient();
                var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, "https://api.payos.vn/v2/payment-requests");
                req.Headers.Add("x-client-id", clientId);
                req.Headers.Add("x-api-key", apiKey);
                req.Content = new System.Net.Http.StringContent(System.Text.Json.JsonSerializer.Serialize(payosBody), System.Text.Encoding.UTF8, "application/json");

                await client.SendAsync(req, ct);
            }
            catch {}
        }

        return $"https://img.vietqr.io/image/{bankCode}-{accountNo}-compact2.png?amount={amount}&addInfo={Uri.EscapeDataString(addInfo)}&accountName={Uri.EscapeDataString(accountName)}";
    }
}
