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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
    ICustomerReviewService? customerReviewService = null) : IInvoiceService
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
        var exists = await db.Invoices.FirstOrDefaultAsync(x => x.SessionId == sessionId && x.Status != 3, ct);
        if (exists is not null) 
        {
            return MapInvoiceDto(exists);
        }

        var session = await db.Sessions.FindAsync([sessionId], ct) ?? throw new NotFoundException("Session not found.");
        
        // If session is still active, close it automatically so everything is calculated
        if (session.Status == 1)
        {
            var sessionService = new PoolHub.Services.Session.SessionService(db);
            await sessionService.CloseAsync(sessionId, issuedByUserId, ct);
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
        var becamePaid = false;
        if (invoice.PaidAmount >= invoice.GrandTotalAmount)
        {
            invoice.PaymentStatus = InvoicePaymentStatuses.Paid;
            invoice.Status = 2; // Completed
            becamePaid = true;
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
                    throw BuildMissingPricingRuleException(table, assignment.StartedAtUtc, "thời điểm bắt đầu gán bàn");
                }
            }
        }
    }

    private async Task<PricingPlanRule?> FindActiveRuleAsync(long tableTypeId, DateTime time, CancellationToken ct)
    {
        var utcTime = NormalizeUtc(time);
        var localTime = ConvertUtcToVenueLocal(utcTime);
        var dayOfWeek = (int)localTime.DayOfWeek;
        var previousDayOfWeek = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
        var timeOfDay = localTime.TimeOfDay;

        var activePlans = await db.PricingPlans
            .Where(p => p.IsActive && p.StartsAtUtc <= utcTime && (p.EndsAtUtc == null || p.EndsAtUtc >= utcTime))
            .ToListAsync(ct);

        if (!activePlans.Any()) return null;

        var planIds = activePlans.OrderByDescending(p => p.IsDefault).Select(p => p.PricingPlanId).ToList();

        foreach (var planId in planIds)
        {
            var candidates = await db.PricingPlanRules
                .Where(r => r.PricingPlanId == planId &&
                            r.TableTypeId == tableTypeId &&
                            r.IsActive &&
                            (r.DayOfWeek == dayOfWeek || r.DayOfWeek == previousDayOfWeek))
                .OrderByDescending(r => r.DayOfWeek == dayOfWeek)
                .ThenByDescending(r => r.PricingPlanRuleId)
                .ToListAsync(ct);
            var rule = candidates.FirstOrDefault(r => RuleMatchesLocalTime(r, dayOfWeek, timeOfDay));
            if (rule != null) return rule;
        }

        return null;
    }

    private static bool RuleMatchesLocalTime(PricingPlanRule rule, int localDayOfWeek, TimeSpan localTime)
    {
        if (rule.StartTime < rule.EndTime)
        {
            return rule.DayOfWeek == localDayOfWeek &&
                   rule.StartTime <= localTime &&
                   localTime < rule.EndTime;
        }

        if (rule.StartTime > rule.EndTime)
        {
            return (rule.DayOfWeek == localDayOfWeek && localTime >= rule.StartTime) ||
                   (NextDay(rule.DayOfWeek) == localDayOfWeek && localTime < rule.EndTime);
        }

        return rule.DayOfWeek == localDayOfWeek;
    }

    private static int NextDay(int dayOfWeek) => dayOfWeek == 6 ? 0 : dayOfWeek + 1;

    private ConflictException BuildMissingPricingRuleException(VenueTable table, DateTime startedAtUtc, string context)
    {
        var utcTime = NormalizeUtc(startedAtUtc);
        var localTime = ConvertUtcToVenueLocal(utcTime);
        var message = $"Không tìm thấy bảng giá đang áp dụng cho bàn {table.TableCode} tại {context}.";
        return new ConflictException(message, [
            $"tableCode={table.TableCode}",
            $"tableTypeId={table.TableTypeId}",
            $"startedAtUtc={utcTime:O}",
            $"venueLocalTime={localTime:O}",
            $"dayOfWeek={(int)localTime.DayOfWeek}",
            $"localTime={localTime.TimeOfDay}"
        ]);
    }

    private DateTime ConvertUtcToVenueLocal(DateTime utcTime)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(NormalizeUtc(utcTime), GetVenueTimeZone());
    }

    private TimeZoneInfo GetVenueTimeZone()
    {
        var configuredId = config?["Venue:TimeZoneId"] ?? "Asia/Ho_Chi_Minh";
        foreach (var id in new[] { configuredId, "Asia/Ho_Chi_Minh", "SE Asia Standard Time" }.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }

    private static DateTime NormalizeUtc(DateTime time) =>
        time.Kind == DateTimeKind.Utc ? time : DateTime.SpecifyKind(time, DateTimeKind.Utc);

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

        return BankTransferQrHelper.BuildVietQrUrl(qrConfig, amount, addInfo);
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
                OrderCode = $"OD{DateTime.UtcNow:yyyyMMddHHmmss}",
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
                if (discount.MinTimeSubtotal.HasValue && invoice.TimeSubtotalAmount < discount.MinTimeSubtotal.Value)
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

        await db.SaveChangesAsync(ct);

        if (posNotificationService != null)
        {
            await posNotificationService.NotifySessionUpdateAsync((int)invoice.SessionId, ct);
        }

        return MapInvoiceDto(invoice);
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

        db.Payments.Add(new Payment
        {
            InvoiceId = invoice.InvoiceId,
            PaymentMethodId = paymentMethod.PaymentMethodId,
            Amount = appliedAmount,
            PaymentStatus = PaymentStatuses.Completed,
            TransactionCode = deposit.TransactionCode,
            PaidAtUtc = DateTime.UtcNow,
            Note = $"Booking deposit applied from booking #{session.BookingId.Value}"
        });

        invoice.PaidAmount += appliedAmount;
        invoice.PaymentStatus = invoice.PaidAmount >= invoice.GrandTotalAmount
            ? InvoicePaymentStatuses.Paid
            : InvoicePaymentStatuses.PartiallyPaid;
        if (invoice.PaymentStatus == InvoicePaymentStatuses.Paid)
        {
            invoice.Status = 2;
        }

        deposit.AppliedAmount = appliedAmount;
        deposit.AppliedToInvoiceId = invoice.InvoiceId;
        if (deposit.PaidAmount > invoice.GrandTotalAmount)
        {
            deposit.RefundedAmount = deposit.PaidAmount - invoice.GrandTotalAmount;
            deposit.RefundedAtUtc = DateTime.UtcNow;
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
}
