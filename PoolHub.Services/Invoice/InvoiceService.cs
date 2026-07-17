using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PoolHub.Core.DTOs.Invoice;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Exceptions;
using PoolHub.Services.Payments;
using PoolHub.Shared.Time;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EntityInvoice = PoolHub.Core.Entities.Invoice;
using EntityOrder = PoolHub.Core.Entities.Order;

namespace PoolHub.Services.Invoice;

public class InvoiceService(
    PoolHubDbContext db, 
    IConfiguration? config = null, 
    IHttpClientFactory? httpClientFactory = null, 
    IPosNotificationService? posNotificationService = null,
    ICustomerReviewService? customerReviewService = null,
    IClock? clock = null) : IInvoiceService
{
    private readonly IClock _clock = clock ?? SystemClock.Instance;

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
            var (fromUtc, toUtc) = BusinessTime.LocalDateRangeToUtc(request.Date.Value);
            query = query.Where(x => x.IssuedAtUtc.HasValue && x.IssuedAtUtc >= fromUtc && x.IssuedAtUtc < toUtc);
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
        var exists = await db.Invoices.FirstOrDefaultAsync(x => x.SessionId == sessionId && x.Status != 3, ct);
        if (exists is not null) 
        {
            return MapInvoiceDto(exists);
        }

        var session = await db.Sessions.FindAsync([sessionId], ct) ?? throw new NotFoundException("Session not found.");
        
        // If session is still active, close it automatically so everything is calculated
        if (session.Status == 1)
        {
            var sessionService = new PoolHub.Services.Session.SessionService(db, posNotificationService ?? new NoOpPosNotificationService(), config, _clock);
            await sessionService.CloseAsync(sessionId, issuedByUserId, ct);
            await db.SaveChangesAsync(ct);
        }

        // Sum amounts
        var productTotal = await db.Orders.Where(o => o.SessionId == sessionId && o.Status != 3).SumAsync(o => o.SubtotalAmount, ct);
        var timeTotal = await db.SessionTableAssignments.Where(x => x.SessionId == sessionId).SumAsync(x => x.Amount ?? 0, ct);
        var subtotal = productTotal + timeTotal;

        var now = _clock.UtcNow;
        var invoice = new EntityInvoice
        {
            SessionId = sessionId,
            CustomerId = session.CustomerId,
            InvoiceCode = $"INV{now:yyyyMMddHHmmss}",
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
            IssuedAtUtc = now
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
                Quantity = assignment.HourlyRateSnapshot > 0 ? (assignment.Amount ?? 0) / assignment.HourlyRateSnapshot : (decimal)(assignment.DurationMinutes ?? 0) / 60m,
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

        await ApplyBookingDepositToInvoiceAsync(session, invoice, ct);

        await db.SaveChangesAsync(ct);
        return MapInvoiceDto(invoice);
    }

    public async Task<CreatePaymentResponse> CreatePaymentAsync(CreatePaymentRequest request, long? receivedByUserId, CancellationToken ct)
    {
        if (request.Amount <= 0)
        {
            throw new ValidationException("Payment amount must be greater than zero.");
        }

        if (request.CustomerId.HasValue || !string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            await UpdateInvoiceCustomerAsync(request.InvoiceId, new UpdateInvoiceCustomerRequest
            {
                CustomerId = request.CustomerId,
                PhoneNumber = request.PhoneNumber,
                FullName = request.CustomerName
            }, receivedByUserId, ct);
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

        var now = _clock.UtcNow;
        var payment = new Payment 
        { 
            InvoiceId = request.InvoiceId, 
            PaymentMethodId = request.PaymentMethodId, 
            Amount = request.Amount, 
            PaymentStatus = PaymentStatuses.Completed,
            TransactionCode = $"TXN{now:HHmmssddMMyyyy}",
            ReceivedByUserId = receivedByUserId, 
            PaidAtUtc = now 
        };
        db.Payments.Add(payment);

        invoice.PaidAmount += request.Amount;
        var becamePaid = false;
        if (invoice.PaidAmount >= invoice.GrandTotalAmount)
        {
            invoice.PaymentStatus = InvoicePaymentStatuses.Paid;
            invoice.Status = 2; // Completed
            becamePaid = true;
            await ProcessInvoicePaidRewardsAsync(invoice, ct);
        }

        await db.SaveChangesAsync(ct);

        PoolHub.Core.DTOs.CustomerReview.ReviewInvitationLinkDto? invitation = null;
        if (becamePaid && customerReviewService is not null)
        {
            try
            {
                invitation = await customerReviewService.CreateInvitationForInvoiceAsync(invoice.InvoiceId, receivedByUserId, ct);
            }
            catch (ConflictException)
            {
                invitation = null;
            }
        }

        return new CreatePaymentResponse
        {
            InvoiceId = invoice.InvoiceId,
            InvoiceCode = invoice.InvoiceCode,
            PaymentStatus = invoice.PaymentStatus,
            PaidAmount = invoice.PaidAmount,
            GrandTotalAmount = invoice.GrandTotalAmount,
            ReviewInvitation = invitation
        };
    }

    public Task<List<PaymentMethodDto>> GetPaymentMethodsAsync(CancellationToken ct)
        => db.PaymentMethods
            .Where(x => x.IsActive && x.Code != "DEPOSIT")
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

        if (invoice.Status != 3 && invoice.PaymentStatus != InvoicePaymentStatuses.Paid && invoice.SessionId > 0)
        {
            var session = await db.Sessions.FindAsync([invoice.SessionId], ct);
            if (session != null && session.BookingId.HasValue)
            {
                await ApplyBookingDepositToInvoiceAsync(session, invoice, ct);
                await db.SaveChangesAsync(ct);
            }
        }

        if (invoice.Status != 3 && invoice.PaymentStatus != InvoicePaymentStatuses.Paid && invoice.GrandTotalAmount > invoice.PaidAmount && httpClientFactory != null)
        {
            try
            {
                var clientId = config?["PayOSSettings:ClientId"];
                var apiKey = config?["PayOSSettings:ApiKey"];
                if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(apiKey))
                {
                    var orderCodesToCheck = new List<long>();
                    if (!string.IsNullOrEmpty(invoice.Note))
                    {
                        var matches = System.Text.RegularExpressions.Regex.Matches(invoice.Note, @"PayOS_OrderCode:(\d+)");
                        for (int i = matches.Count - 1; i >= 0; i--)
                        {
                            if (long.TryParse(matches[i].Groups[1].Value, out var code) && !orderCodesToCheck.Contains(code))
                            {
                                orderCodesToCheck.Add(code);
                            }
                        }
                    }
                    if (!orderCodesToCheck.Contains(id)) orderCodesToCheck.Add(id);

                    var client = httpClientFactory.CreateClient();
                    foreach (var checkCode in orderCodesToCheck)
                    {
                        var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, $"https://api-merchant.payos.vn/v2/payment-requests/{checkCode}?_t={DateTime.UtcNow.Ticks}");
                        req.Headers.Add("x-client-id", clientId);
                        req.Headers.Add("x-api-key", apiKey);
                        req.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
                        req.Headers.Add("Pragma", "no-cache");
                        var res = await client.SendAsync(req, ct);
                        if (res.IsSuccessStatusCode)
                        {
                            var resStr = await res.Content.ReadAsStringAsync(ct);
                            using var doc = System.Text.Json.JsonDocument.Parse(resStr);
                            if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == System.Text.Json.JsonValueKind.Object)
                            {
                                var status = data.TryGetProperty("status", out var st) ? st.GetString() : null;
                                decimal amountPaid = 0m;
                                if (data.TryGetProperty("amountPaid", out var ap) && ap.ValueKind == System.Text.Json.JsonValueKind.Number)
                                {
                                    ap.TryGetDecimal(out amountPaid);
                                }
                                decimal amount = 0m;
                                if (data.TryGetProperty("amount", out var am) && am.ValueKind == System.Text.Json.JsonValueKind.Number)
                                {
                                    am.TryGetDecimal(out amount);
                                }

                                if (string.Equals(status, "PAID", StringComparison.OrdinalIgnoreCase) || (amountPaid > 0 && amountPaid >= (invoice.GrandTotalAmount - invoice.PaidAmount)))
                                {
                                    var payAmount = amountPaid > 0 ? amountPaid : (amount > 0 ? amount : (invoice.GrandTotalAmount - invoice.PaidAmount));
                                    var bankMethod = await db.PaymentMethods.FirstOrDefaultAsync(x => x.Code == "BANK" || x.Name.Contains("Chuyển") || x.Name.Contains("QR") || x.Name.Contains("Bank"), ct);
                                    var methodId = bankMethod?.PaymentMethodId ?? 1;

                                    await CreatePaymentAsync(new CreatePaymentRequest
                                    {
                                        InvoiceId = id,
                                        PaymentMethodId = methodId,
                                        Amount = payAmount
                                    }, null, ct);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch {}
        }
        
        var lines = await (from line in db.InvoiceLines
                           where line.InvoiceId == id
                           join item in db.OrderItems on line.ReferenceId equals item.OrderItemId into items
                           from item in items.DefaultIfEmpty()
                           select new InvoiceLineDto
                           {
                               InvoiceLineId = line.InvoiceLineId,
                               InvoiceId = line.InvoiceId,
                               LineType = line.LineType,
                               ReferenceId = line.ReferenceId,
                               Description = line.Description,
                               Quantity = line.Quantity,
                               UnitPrice = line.UnitPrice,
                               LineTotalAmount = line.LineTotalAmount,
                               ProductId = line.LineType == "PRODUCT" ? (item != null ? item.ProductId : (long?)null) : (long?)null
                           }).ToListAsync(ct);

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
            DepositAppliedAmount = await db.BookingDeposits.AsNoTracking().Where(x => x.AppliedToInvoiceId == invoice.InvoiceId).SumAsync(x => x.AppliedAmount, ct),
            DepositRefundAmount = await db.BookingDeposits.AsNoTracking().Where(x => x.AppliedToInvoiceId == invoice.InvoiceId).SumAsync(x => x.RefundedAmount, ct),
            RemainingAmount = Math.Max(0, invoice.GrandTotalAmount - invoice.PaidAmount),
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
        var now = _clock.UtcNow;
        var discount = await db.Discounts
            .FirstOrDefaultAsync(d => d.DiscountCode.ToUpper() == code, ct)
            ?? throw new NotFoundException("Mã giảm giá không tồn tại trên hệ thống. (Lưu ý: Nếu dùng voucher đổi điểm, vui lòng lấy mã cá nhân có dạng V-xxx trong Lịch sử tích điểm).");

        if (!discount.IsActive)
        {
            throw new BusinessRuleException("Mã giảm giá này đang bị vô hiệu hóa hoặc chưa kích hoạt.");
        }
        if (discount.StartsAtUtc > now)
        {
            throw new BusinessRuleException("Mã giảm giá này chưa đến thời gian có hiệu lực.");
        }
        if (discount.EndsAtUtc.HasValue && discount.EndsAtUtc.Value < now)
        {
            throw new BusinessRuleException("Mã giảm giá này đã hết hạn sử dụng!");
        }

        if (discount.CustomerId.HasValue)
        {
            if (!invoice.CustomerId.HasValue)
            {
                throw new BusinessRuleException("Voucher này là voucher đổi thưởng cá nhân. Vui lòng chọn khách hàng (người chơi) cho hóa đơn trước khi áp dụng.");
            }
            if (invoice.CustomerId.Value != discount.CustomerId.Value)
            {
                throw new BusinessRuleException("Voucher này chỉ có người chơi đã đổi thưởng mới có thể sử dụng!");
            }
        }

        if (discount.MaxUsage > 0)
        {
            if (discount.UsageCount >= discount.MaxUsage)
            {
                throw new BusinessRuleException("Voucher này đã được sử dụng hoặc đã hết lượt áp dụng!");
            }

            var activeUsages = await db.InvoiceDiscounts
                .Where(x => x.DiscountId == discount.DiscountId)
                .CountAsync(ct);
            if (activeUsages >= discount.MaxUsage)
            {
                throw new BusinessRuleException("Voucher này đang được áp dụng cho một hóa đơn khác hoặc đã hết lượt áp dụng!");
            }
        }

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

        if (invoice.SessionId > 0)
        {
            var session = await db.Sessions.FindAsync([invoice.SessionId], ct);
            if (session != null && session.BookingId.HasValue)
            {
                await ApplyBookingDepositToInvoiceAsync(session, invoice, ct);
            }
        }

        if (invoice.PaidAmount >= invoice.GrandTotalAmount && invoice.GrandTotalAmount > 0)
        {
            invoice.PaymentStatus = InvoicePaymentStatuses.Paid;
            invoice.Status = 2; // Completed
        }
        else if (invoice.PaidAmount > 0)
        {
            invoice.PaymentStatus = InvoicePaymentStatuses.PartiallyPaid;
            if (invoice.Status == 2) invoice.Status = 1;
        }
        else
        {
            invoice.PaymentStatus = InvoicePaymentStatuses.Unpaid;
            if (invoice.Status == 2) invoice.Status = 1;
        }

        ClearPayOsNoteCache(invoice);
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

        if (invoice.SessionId > 0)
        {
            var session = await db.Sessions.FindAsync([invoice.SessionId], ct);
            if (session != null && session.BookingId.HasValue)
            {
                await ApplyBookingDepositToInvoiceAsync(session, invoice, ct);
            }
        }

        if (invoice.PaidAmount >= invoice.GrandTotalAmount && invoice.GrandTotalAmount > 0)
        {
            invoice.PaymentStatus = InvoicePaymentStatuses.Paid;
            invoice.Status = 2; // Completed
        }
        else if (invoice.PaidAmount > 0)
        {
            invoice.PaymentStatus = InvoicePaymentStatuses.PartiallyPaid;
            if (invoice.Status == 2) invoice.Status = 1;
        }
        else
        {
            invoice.PaymentStatus = InvoicePaymentStatuses.Unpaid;
            if (invoice.Status == 2) invoice.Status = 1;
        }

        ClearPayOsNoteCache(invoice);
        await db.SaveChangesAsync(ct);
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
        
        var existingDiscounts = await db.InvoiceDiscounts.Where(x => x.InvoiceId == invoiceId).ToListAsync(ct);
        if (existingDiscounts.Count > 0)
        {
            db.InvoiceDiscounts.RemoveRange(existingDiscounts);
        }

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
        var bankMethod = await db.PaymentMethods
            .AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(ct);
        var qrConfig = bankMethod
            .Select(BankTransferQrHelper.Parse)
            .FirstOrDefault(x => x is not null && x.CanBuildDynamicQr)
            ?? throw new BusinessRuleException("Active bank transfer payment method is not configured for VietQR.");
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
                string? payosQrString = null;

                if (!string.IsNullOrEmpty(invoice.Note))
                {
                    var matchOrder = System.Text.RegularExpressions.Regex.Matches(invoice.Note, @"PayOS_OrderCode:(\d+)").LastOrDefault();
                    if (matchOrder != null && matchOrder.Success && long.TryParse(matchOrder.Groups[1].Value, out var savedOrderCode))
                    {
                        orderCode = savedOrderCode;
                    }

                    var matchQr = System.Text.RegularExpressions.Regex.Matches(invoice.Note, @"PayOS_QrCode:([^\s|]+)").LastOrDefault();
                    if (matchQr != null && matchQr.Success)
                    {
                        payosQrString = matchQr.Groups[1].Value;
                    }
                }

                var client = httpClientFactory.CreateClient();

                // 1. Thử tạo link thanh toán trên PayOS
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

                var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, "https://api-merchant.payos.vn/v2/payment-requests");
                req.Headers.Add("x-client-id", clientId);
                req.Headers.Add("x-api-key", apiKey);
                req.Content = new System.Net.Http.StringContent(System.Text.Json.JsonSerializer.Serialize(payosBody), System.Text.Encoding.UTF8, "application/json");

                var res = await client.SendAsync(req, ct);
                var resStr = await res.Content.ReadAsStringAsync(ct);

                if (res.IsSuccessStatusCode && !resStr.Contains("\"code\":\"233\""))
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(resStr);
                    if (doc.RootElement.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        if (dataEl.TryGetProperty("qrCode", out var qrEl) && qrEl.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            payosQrString = qrEl.GetString();
                        }
                    }

                    ClearPayOsNoteCache(invoice);
                    invoice.Note = string.IsNullOrWhiteSpace(invoice.Note) ? $"PayOS_OrderCode:{orderCode}" : $"{invoice.Note} | PayOS_OrderCode:{orderCode}";
                    if (!string.IsNullOrWhiteSpace(payosQrString))
                    {
                        invoice.Note = $"{invoice.Note} | PayOS_QrCode:{payosQrString}";
                    }
                    await db.SaveChangesAsync(ct);
                }

                // 2. Nếu PayOS báo lỗi mã đơn hàng đã tồn tại (code 233)
                if (resStr.Contains("\"code\":\"233\"") || resStr.Contains("\"233\"") || resStr.Contains("tồn tại") || resStr.Contains("exists"))
                {
                    try
                    {
                        // Kiểm tra trạng thái link cũ trên PayOS
                        var checkReq = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, $"https://api-merchant.payos.vn/v2/payment-requests/{orderCode}");
                        checkReq.Headers.Add("x-client-id", clientId);
                        checkReq.Headers.Add("x-api-key", apiKey);
                        var checkRes = await client.SendAsync(checkReq, ct);
                        bool needNewOrderCode = true;

                        if (checkRes.IsSuccessStatusCode)
                        {
                            var checkStr = await checkRes.Content.ReadAsStringAsync(ct);
                            using var checkDoc = System.Text.Json.JsonDocument.Parse(checkStr);
                            if (checkDoc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == System.Text.Json.JsonValueKind.Object)
                            {
                                var st = data.TryGetProperty("status", out var stProp) ? stProp.GetString() : null;
                                decimal existingAmt = 0m;
                                if (data.TryGetProperty("amount", out var amProp) && amProp.ValueKind == System.Text.Json.JsonValueKind.Number)
                                {
                                    amProp.TryGetDecimal(out existingAmt);
                                }

                                if (string.Equals(st, "PENDING", StringComparison.OrdinalIgnoreCase) && existingAmt == amount)
                                {
                                    if (data.TryGetProperty("qrCode", out var qrEl) && qrEl.ValueKind == System.Text.Json.JsonValueKind.String)
                                    {
                                        payosQrString = qrEl.GetString();
                                    }
                                    if (string.IsNullOrWhiteSpace(payosQrString) && !string.IsNullOrWhiteSpace(invoice.Note))
                                    {
                                        var matchQr = System.Text.RegularExpressions.Regex.Match(invoice.Note, @"PayOS_QrCode:([^\s|]+)");
                                        if (matchQr.Success)
                                        {
                                            payosQrString = matchQr.Groups[1].Value;
                                        }
                                    }
                                    if (!string.IsNullOrWhiteSpace(payosQrString))
                                    {
                                        needNewOrderCode = false; // Link cũ vẫn PENDING và có sẵn chuỗi PayOS QR, dùng tiếp
                                    }
                                }
                            }
                        }

                        // Nếu link cũ đã bị HỦY, HẾT HẠN, đã thanh toán trên PayOS (trong khi hoá đơn chưa thanh toán) hoặc không lấy được qrCode -> Tạo orderCode mới duy nhất
                        if (needNewOrderCode)
                        {
                            long newOrderCode = long.Parse($"{DateTime.UtcNow:yyMMddHHmmss}{Random.Shared.Next(10, 99)}");
                            string newRawData = $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={newOrderCode}&returnUrl={returnUrl}";
                            using var newHmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(checksumKey));
                            var newHash = newHmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(newRawData));
                            var newSignature = BitConverter.ToString(newHash).Replace("-", "").ToLower();

                            var newBody = new
                            {
                                orderCode = newOrderCode,
                                amount = amount,
                                description = description,
                                cancelUrl = cancelUrl,
                                returnUrl = returnUrl,
                                signature = newSignature
                            };

                            var retryReq = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, "https://api-merchant.payos.vn/v2/payment-requests");
                            retryReq.Headers.Add("x-client-id", clientId);
                            retryReq.Headers.Add("x-api-key", apiKey);
                            retryReq.Content = new System.Net.Http.StringContent(System.Text.Json.JsonSerializer.Serialize(newBody), System.Text.Encoding.UTF8, "application/json");
                            var retryRes = await client.SendAsync(retryReq, ct);
                            var retryStr = await retryRes.Content.ReadAsStringAsync(ct);

                            if (retryRes.IsSuccessStatusCode && !retryStr.Contains("\"code\":\"233\""))
                            {
                                using var retryDoc = System.Text.Json.JsonDocument.Parse(retryStr);
                                if (retryDoc.RootElement.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == System.Text.Json.JsonValueKind.Object)
                                {
                                    if (dataEl.TryGetProperty("qrCode", out var qrEl) && qrEl.ValueKind == System.Text.Json.JsonValueKind.String)
                                    {
                                        payosQrString = qrEl.GetString();
                                    }
                                }

                                ClearPayOsNoteCache(invoice);
                                invoice.Note = string.IsNullOrWhiteSpace(invoice.Note) ? $"PayOS_OrderCode:{newOrderCode}" : $"{invoice.Note} | PayOS_OrderCode:{newOrderCode}";
                                if (!string.IsNullOrWhiteSpace(payosQrString))
                                {
                                    invoice.Note = $"{invoice.Note} | PayOS_QrCode:{payosQrString}";
                                }
                                await db.SaveChangesAsync(ct);
                            }
                        }
                    }
                    catch {}
                }

                if (!string.IsNullOrWhiteSpace(payosQrString))
                {
                    return $"https://api.qrserver.com/v1/create-qr-code/?size=400x400&data={Uri.EscapeDataString(payosQrString)}";
                }
            }
            catch {}
        }

        throw new BusinessRuleException("Hệ thống hiện tại chỉ hỗ trợ thanh toán chuyển khoản qua cổng PayOS. Vui lòng kiểm tra lại cấu hình PayOS hoặc liên hệ quản trị viên.");
    }

    private static void ClearPayOsNoteCache(EntityInvoice invoice)
    {
        if (!string.IsNullOrWhiteSpace(invoice.Note))
        {
            invoice.Note = System.Text.RegularExpressions.Regex.Replace(invoice.Note, @"PayOS_OrderCode:\d+\s*\|?\s*", "").Trim(' ', '|');
            invoice.Note = System.Text.RegularExpressions.Regex.Replace(invoice.Note, @"PayOS_QrCode:[^\s|]+\s*\|?\s*", "").Trim(' ', '|');
            if (string.IsNullOrWhiteSpace(invoice.Note)) invoice.Note = null;
        }
    }

    public async Task<InvoiceDto> UpdateInvoiceProductsAsync(long id, UpdateInvoiceProductsRequest request, long? userId, CancellationToken ct)
    {
        var invoice = await db.Invoices.FindAsync([id], ct) ?? throw new NotFoundException("Invoice not found.");
        if (invoice.Status == 3)
        {
            throw new BusinessRuleException("Cannot modify a cancelled invoice.");
        }
        if (invoice.PaymentStatus == InvoicePaymentStatuses.Paid)
        {
            throw new BusinessRuleException("Cannot modify a fully paid invoice.");
        }

        var session = await db.Sessions.FindAsync([invoice.SessionId], ct) ?? throw new NotFoundException("Session not found.");

        var productIds = request.Products.Select(p => p.ProductId).Distinct().ToList();
        var productsDb = await db.Products.Where(p => productIds.Contains(p.ProductId)).ToDictionaryAsync(p => p.ProductId, ct);

        var invoiceLines = await db.InvoiceLines.Where(il => il.InvoiceId == id).ToListAsync(ct);

        var orders = await db.Orders.Where(o => o.SessionId == invoice.SessionId && o.Status != 3).ToListAsync(ct);
        EntityOrder order;
        if (orders.Count > 0)
        {
            order = orders[0];
        }
        else
        {
            order = new EntityOrder
            {
                SessionId = invoice.SessionId,
                OrderedByUserId = userId ?? 1,
                OrderCode = $"OD{_clock.UtcNow:yyyyMMddHHmmss}",
                Status = 1,
                SubtotalAmount = 0
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync(ct);
        }

        var orderItems = await db.OrderItems.Where(oi => oi.OrderId == order.OrderId).ToListAsync(ct);
        var requestedProducts = request.Products
            .GroupBy(p => p.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToList();

        var processedProductIds = new HashSet<long>();

        foreach (var reqProd in requestedProducts)
        {
            var productId = reqProd.ProductId;
            var newQty = reqProd.Quantity;
            if (!productsDb.TryGetValue(productId, out var product))
            {
                throw new NotFoundException($"Product with ID {productId} not found.");
            }
            if (!product.IsActive)
            {
                throw new BusinessRuleException($"Product '{product.Name}' is inactive.");
            }

            processedProductIds.Add(productId);

            var orderItem = orderItems.FirstOrDefault(oi => oi.ProductId == productId);
            int oldQty = orderItem?.Quantity ?? 0;
            int diff = newQty - oldQty;

            if (diff == 0) continue;

            if (diff > 0)
            {
                if (product.IsStockTracked && product.StockQuantity < diff)
                {
                    throw new BusinessRuleException($"Sản phẩm '{product.Name}' không đủ số lượng trong kho. Hiện có: {product.StockQuantity}, cần thêm: {diff}.");
                }
                if (product.IsStockTracked)
                {
                    product.StockQuantity -= diff;
                }
            }
            else
            {
                if (product.IsStockTracked)
                {
                    product.StockQuantity += -diff;
                }
            }

            if (orderItem != null)
            {
                if (newQty > 0)
                {
                    orderItem.Quantity = newQty;
                    orderItem.LineTotalAmount = newQty * product.UnitPrice;
                }
                else
                {
                    db.OrderItems.Remove(orderItem);
                }
            }
            else if (newQty > 0)
            {
                orderItem = new OrderItem
                {
                    OrderId = order.OrderId,
                    ProductId = productId,
                    ProductNameSnapshot = product.Name,
                    Quantity = newQty,
                    UnitPriceSnapshot = product.UnitPrice,
                    LineTotalAmount = newQty * product.UnitPrice
                };
                db.OrderItems.Add(orderItem);
            }

            await db.SaveChangesAsync(ct);

            var invoiceLine = invoiceLines.FirstOrDefault(il => il.LineType == "PRODUCT" && il.ReferenceId == orderItem?.OrderItemId);
            if (invoiceLine != null)
            {
                if (newQty > 0)
                {
                    invoiceLine.Quantity = newQty;
                    invoiceLine.UnitPrice = product.UnitPrice;
                    invoiceLine.LineTotalAmount = newQty * product.UnitPrice;
                }
                else
                {
                    db.InvoiceLines.Remove(invoiceLine);
                }
            }
            else if (newQty > 0 && orderItem != null)
            {
                invoiceLine = new InvoiceLine
                {
                    InvoiceId = id,
                    LineType = "PRODUCT",
                    ReferenceId = orderItem.OrderItemId,
                    Description = product.Name,
                    Quantity = newQty,
                    UnitPrice = product.UnitPrice,
                    LineTotalAmount = newQty * product.UnitPrice
                };
                db.InvoiceLines.Add(invoiceLine);
            }

            db.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = productId,
                TransactionType = 4, // Sale
                Quantity = -diff,
                ReferenceType = "ORDER",
                ReferenceId = order.OrderId,
                Note = $"INVOICE PRODUCT QUANTITY UPDATED (diff: {diff})",
                CreatedByUserId = userId
            });
        }

        // Clean up products that were NOT in the request
        var invoiceLinesToDelete = invoiceLines
            .Where(il => il.LineType == "PRODUCT")
            .Where(il => !processedProductIds.Contains(db.OrderItems.FirstOrDefault(oi => oi.OrderItemId == il.ReferenceId)?.ProductId ?? 0))
            .ToList();

        foreach (var il in invoiceLinesToDelete)
        {
            var orderItem = orderItems.FirstOrDefault(oi => oi.OrderItemId == il.ReferenceId);
            if (orderItem != null)
            {
                var product = await db.Products.FindAsync([orderItem.ProductId], ct);
                if (product != null)
                {
                    if (product.IsStockTracked)
                    {
                        product.StockQuantity += orderItem.Quantity;
                    }

                    db.InventoryTransactions.Add(new InventoryTransaction
                    {
                        ProductId = product.ProductId,
                        TransactionType = 4,
                        Quantity = orderItem.Quantity,
                        ReferenceType = "ORDER",
                        ReferenceId = order.OrderId,
                        Note = "INVOICE PRODUCT REMOVED",
                        CreatedByUserId = userId
                    });
                }
                db.OrderItems.Remove(orderItem);
            }
            db.InvoiceLines.Remove(il);
        }

        await db.SaveChangesAsync(ct);

        var finalOrderItems = await db.OrderItems.Where(oi => oi.OrderId == order.OrderId).ToListAsync(ct);
        order.SubtotalAmount = finalOrderItems.Sum(oi => oi.LineTotalAmount);

        var currentLines = await db.InvoiceLines.Where(il => il.InvoiceId == id).ToListAsync(ct);
        var newProductTotal = currentLines.Where(il => il.LineType == "PRODUCT").Sum(il => il.LineTotalAmount);
        var newTimeTotal = currentLines.Where(il => il.LineType == "TIME").Sum(il => il.LineTotalAmount);

        invoice.ProductSubtotalAmount = newProductTotal;
        invoice.TimeSubtotalAmount = newTimeTotal;
        invoice.SubtotalAmount = newProductTotal + newTimeTotal;

        var existingDiscount = await db.InvoiceDiscounts.FirstOrDefaultAsync(x => x.InvoiceId == id, ct);
        if (existingDiscount != null)
        {
            var discount = await db.Discounts.FindAsync([existingDiscount.DiscountId], ct);
            if (discount != null)
            {
                if ((discount.MinTimeSubtotal.HasValue && invoice.TimeSubtotalAmount < discount.MinTimeSubtotal.Value)
                    || (discount.CustomerId.HasValue && invoice.CustomerId != discount.CustomerId.Value))
                {
                    db.InvoiceDiscounts.Remove(existingDiscount);
                    invoice.DiscountAmount = 0;
                }
                else
                {
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

                    existingDiscount.AmountApplied = discountAmt;
                    invoice.DiscountAmount = discountAmt;
                }
            }
        }

        invoice.GrandTotalAmount = Math.Max(0, invoice.SubtotalAmount - invoice.DiscountAmount + invoice.TaxAmount);

        if (invoice.PaidAmount >= invoice.GrandTotalAmount)
        {
            invoice.PaymentStatus = InvoicePaymentStatuses.Paid;
            invoice.Status = 2; // Completed
        }
        else if (invoice.PaidAmount > 0)
        {
            invoice.PaymentStatus = InvoicePaymentStatuses.PartiallyPaid;
        }
        else
        {
            invoice.PaymentStatus = InvoicePaymentStatuses.Unpaid;
        }

        ClearPayOsNoteCache(invoice);
        await db.SaveChangesAsync(ct);

        if (posNotificationService != null)
        {
            await posNotificationService.NotifySessionUpdateAsync((int)invoice.SessionId, ct);
        }

        return MapInvoiceDto(invoice);
    }

    public async Task<InvoiceDetailDto> UpdateInvoiceCustomerAsync(long id, UpdateInvoiceCustomerRequest request, long? userId, CancellationToken ct)
    {
        var invoice = await db.Invoices.FindAsync([id], ct) ?? throw new NotFoundException("Invoice not found.");
        if (invoice.Status == 3) throw new BusinessRuleException("Cannot modify a cancelled invoice.");
        if (invoice.PaymentStatus == InvoicePaymentStatuses.Paid) throw new BusinessRuleException("Cannot modify a fully paid invoice.");

        PoolHub.Core.Entities.Customer? customer = null;
        if (request.CustomerId.HasValue)
        {
            customer = await db.Customers.FindAsync([request.CustomerId.Value], ct)
                ?? throw new NotFoundException("Khách hàng không tồn tại.");
        }
        else if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var phone = request.PhoneNumber.Trim();
            customer = await db.Customers.FirstOrDefaultAsync(c => c.PhoneNumber == phone && c.Status, ct);
            if (customer == null)
            {
                customer = new PoolHub.Core.Entities.Customer
                {
                    FullName = !string.IsNullOrWhiteSpace(request.FullName) ? request.FullName.Trim() : $"Khách hàng ({phone})",
                    PhoneNumber = phone,
                    Status = true,
                    CreatedAtUtc = _clock.UtcNow
                };
                db.Customers.Add(customer);
                await db.SaveChangesAsync(ct);
            }
        }

        if (invoice.CustomerId != customer?.CustomerId)
        {
            invoice.CustomerId = customer?.CustomerId;
            if (invoice.SessionId > 0)
            {
                var session = await db.Sessions.FindAsync([invoice.SessionId], ct);
                if (session != null) session.CustomerId = customer?.CustomerId;
            }

            var existingDiscount = await db.InvoiceDiscounts.FirstOrDefaultAsync(x => x.InvoiceId == id, ct);
            if (existingDiscount != null)
            {
                var discount = await db.Discounts.FindAsync([existingDiscount.DiscountId], ct);
                if (discount != null && discount.CustomerId.HasValue && discount.CustomerId != invoice.CustomerId)
                {
                    db.InvoiceDiscounts.Remove(existingDiscount);
                    invoice.DiscountAmount = 0;
                    invoice.GrandTotalAmount = Math.Max(0, invoice.SubtotalAmount - invoice.DiscountAmount + invoice.TaxAmount);
                }
            }

            await db.SaveChangesAsync(ct);
        }

        return await GetInvoiceDetailAsync(id, ct);
    }

    private async Task ApplyBookingDepositToInvoiceAsync(PoolHub.Core.Entities.Session session, EntityInvoice invoice, CancellationToken ct)
    {
        if (!session.BookingId.HasValue) return;

        var deposit = await db.BookingDeposits.FirstOrDefaultAsync(x =>
            x.BookingId == session.BookingId.Value &&
            x.Status == BookingDepositStatuses.Paid &&
            x.PaidAmount > 0, ct);
        if (deposit is null) return;

        var appliedAmount = Math.Min(deposit.PaidAmount, invoice.GrandTotalAmount);
        if (appliedAmount <= 0) return;

        var paymentMethod = await db.PaymentMethods.FirstOrDefaultAsync(x => x.Code == "DEPOSIT", ct);
        if (paymentMethod is null)
        {
            paymentMethod = new PaymentMethod
            {
                Code = "DEPOSIT",
                Name = "Deposit Applied",
                Description = "System payment method used when applying booking deposits to invoices.",
                IsActive = true
            };
            db.PaymentMethods.Add(paymentMethod);
            await db.SaveChangesAsync(ct);
        }

        if (await db.Payments.AnyAsync(x => x.InvoiceId == invoice.InvoiceId && x.PaymentMethodId == paymentMethod.PaymentMethodId, ct))
            return;

        db.Payments.Add(new Payment
        {
            InvoiceId = invoice.InvoiceId,
            PaymentMethodId = paymentMethod.PaymentMethodId,
            Amount = appliedAmount,
            PaymentStatus = PaymentStatuses.Completed,
            TransactionCode = $"DEPAPP{_clock.UtcNow:HHmmssddMMyyyy}",
            PaidAtUtc = _clock.UtcNow,
            Note = $"Booking deposit applied from booking #{session.BookingId.Value}"
        });

        invoice.PaidAmount += appliedAmount;
        invoice.PaymentStatus = invoice.PaidAmount >= invoice.GrandTotalAmount
            ? InvoicePaymentStatuses.Paid
            : InvoicePaymentStatuses.PartiallyPaid;
        if (invoice.PaymentStatus == InvoicePaymentStatuses.Paid)
        {
            invoice.Status = 2;
            await ProcessInvoicePaidRewardsAsync(invoice, ct);
        }

        deposit.AppliedAmount = appliedAmount;
        deposit.AppliedToInvoiceId = invoice.InvoiceId;
        if (deposit.PaidAmount > invoice.GrandTotalAmount)
        {
            deposit.RefundedAmount = deposit.PaidAmount - invoice.GrandTotalAmount;
            deposit.RefundedAtUtc = _clock.UtcNow;
            deposit.Status = BookingDepositStatuses.PartiallyRefunded;
        }
        else
        {
            deposit.Status = BookingDepositStatuses.AppliedToInvoice;
        }
    }

    private static InvoiceDto MapInvoiceDto(EntityInvoice invoice) => new()
    {
        InvoiceId = invoice.InvoiceId,
        SessionId = invoice.SessionId,
        InvoiceCode = invoice.InvoiceCode,
        GrandTotalAmount = invoice.GrandTotalAmount,
        PaymentStatus = invoice.PaymentStatus,
        Status = invoice.Status,
        PaidAmount = invoice.PaidAmount,
        RemainingAmount = Math.Max(0, invoice.GrandTotalAmount - invoice.PaidAmount)
    };

    private async Task ProcessInvoicePaidRewardsAsync(EntityInvoice invoice, CancellationToken ct)
    {
        var appliedDiscounts = await db.InvoiceDiscounts.Where(x => x.InvoiceId == invoice.InvoiceId).ToListAsync(ct);
        foreach (var ad in appliedDiscounts)
        {
            var disc = await db.Discounts.FindAsync([ad.DiscountId], ct);
            if (disc != null)
            {
                disc.UsageCount++;
                if (disc.MaxUsage > 0 && disc.UsageCount >= disc.MaxUsage)
                {
                    disc.IsActive = false;
                }
                db.Discounts.Update(disc);
            }
        }

        if (invoice.CustomerId.HasValue)
        {
            var customer = await db.Customers.FindAsync([invoice.CustomerId.Value], ct);
            if (customer != null && customer.Status)
            {
                int earnedPoints = (int)(invoice.GrandTotalAmount / 1000m);
                if (earnedPoints > 0)
                {
                    customer.LoyaltyPoints += earnedPoints;
                    customer.TotalPointsEarned += earnedPoints;
                    db.Customers.Update(customer);

                    db.CustomerPointHistories.Add(new CustomerPointHistory
                    {
                        CustomerId = customer.CustomerId,
                        Points = earnedPoints,
                        TransactionType = "EARN",
                        Description = $"Tích điểm từ hóa đơn {invoice.InvoiceCode} ({invoice.GrandTotalAmount:N0} VND)",
                        ReferenceId = invoice.InvoiceId,
                        CreatedAtUtc = _clock.UtcNow
                    });
                }
            }
        }
    }

    private sealed class NoOpPosNotificationService : IPosNotificationService
    {
        public Task NotifyTableUpdateAsync(int tableId, CancellationToken ct = default) => Task.CompletedTask;
        public Task NotifyBookingUpdateAsync(int bookingId, CancellationToken ct = default) => Task.CompletedTask;
        public Task NotifySessionUpdateAsync(int sessionId, CancellationToken ct = default) => Task.CompletedTask;
        public Task NotifyRefreshPosAsync(CancellationToken ct = default) => Task.CompletedTask;
    }
}
