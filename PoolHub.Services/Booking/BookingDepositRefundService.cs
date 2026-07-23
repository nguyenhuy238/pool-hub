using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PoolHub.Core.DTOs.BookingDepositRefund;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Exceptions;
using PoolHub.Shared.Time;
using EntitySession = PoolHub.Core.Entities.Session;
using EntityInvoice = PoolHub.Core.Entities.Invoice;

namespace PoolHub.Services.Booking;

public class BookingDepositRefundService(PoolHubDbContext db, IAuditService? auditService = null, IClock? clock = null) : IBookingDepositRefundService
{
    private readonly IClock _clock = clock ?? SystemClock.Instance;
    private static readonly int[] BalanceHoldingStatuses =
    [
        BookingDepositRefundStatuses.PendingCustomerInfo,
        BookingDepositRefundStatuses.PendingApproval,
        BookingDepositRefundStatuses.Approved,
        BookingDepositRefundStatuses.Processing,
        BookingDepositRefundStatuses.ReadyForCashPickup,
        BookingDepositRefundStatuses.Succeeded
    ];

    public async Task<decimal> CalculateRefundableBalanceAsync(long bookingDepositId, CancellationToken ct)
    {
        var deposit = await db.BookingDeposits.AsNoTracking().FirstOrDefaultAsync(x => x.BookingDepositId == bookingDepositId, ct)
            ?? throw new NotFoundException("Booking deposit not found.");
        var reserved = await db.BookingDepositRefunds.AsNoTracking()
            .Where(x => x.BookingDepositId == bookingDepositId && BalanceHoldingStatuses.Contains(x.Status))
            .SumAsync(x => x.Amount, ct);

        return Math.Max(0, deposit.PaidAmount - deposit.AppliedAmount - deposit.ForfeitedAmount - reserved);
    }

    public async Task<BookingDepositRefundDto> CreateRefundRequestAsync(CreateBookingDepositRefundRequest request, CancellationToken ct)
    {
        if (request.Amount <= 0) throw new ValidationException("Refund amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new ValidationException("Refund reason is required.");
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey)) throw new ValidationException("Refund idempotency key is required.");

        var existing = await db.BookingDepositRefunds.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdempotencyKey == request.IdempotencyKey, ct);
        if (existing is not null) return Map(existing);

        await using var transaction = await BeginTransactionIfSupportedAsync(ct);
        existing = await db.BookingDepositRefunds
            .FirstOrDefaultAsync(x => x.IdempotencyKey == request.IdempotencyKey, ct);
        if (existing is not null)
        {
            if (transaction is not null) await transaction.CommitAsync(ct);
            return Map(existing);
        }

        var deposit = await db.BookingDeposits.FirstOrDefaultAsync(x => x.BookingDepositId == request.BookingDepositId, ct)
            ?? throw new NotFoundException("Booking deposit not found.");
        if (!HasUsablePaidDeposit(deposit))
            throw new BusinessRuleException("Booking deposit has not been paid and cannot be refunded.");

        var balance = await CalculateRefundableBalanceAsync(deposit.BookingDepositId, ct);
        if (request.Amount > balance)
            throw new BusinessRuleException("Refund amount exceeds the refundable deposit balance.");

        var booking = await db.Bookings.AsNoTracking().FirstOrDefaultAsync(x => x.BookingId == deposit.BookingId, ct)
            ?? throw new NotFoundException("Booking not found.");
        var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.CustomerId == booking.CustomerId, ct);

        var refund = new BookingDepositRefund
        {
            BookingDepositId = deposit.BookingDepositId,
            BookingId = booking.BookingId,
            InvoiceId = request.InvoiceId,
            CustomerId = booking.CustomerId,
            RefundCode = await NextRefundCodeAsync(ct),
            Amount = request.Amount,
            Status = BookingDepositRefundStatuses.PendingCustomerInfo,
            Reason = request.Reason.Trim(),
            RefundMethod = request.RefundMethod,
            ReasonDetail = request.ReasonDetail?.Trim(),
            CustomerEmailSnapshot = customer?.Email,
            CustomerPhoneSnapshot = customer?.PhoneNumber,
            RequestedByUserId = request.RequestedByUserId,
            IdempotencyKey = request.IdempotencyKey.Trim()
        };
        db.BookingDepositRefunds.Add(refund);
        await db.SaveChangesAsync(ct);
        await LogAsync(request.RequestedByUserId, AuditActions.BookingDepositRefundCreated, refund, ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return Map(refund);
    }

    public async Task<BookingDepositRefundDto?> CreateEligibleCancellationRefundAsync(long bookingDepositId, long? requestedByUserId, CancellationToken ct)
    {
        var deposit = await db.BookingDeposits.AsNoTracking().FirstOrDefaultAsync(x => x.BookingDepositId == bookingDepositId, ct)
            ?? throw new NotFoundException("Booking deposit not found.");
        var amount = await CalculateRefundableBalanceAsync(bookingDepositId, ct);
        if (amount <= 0) return null;
        return await CreateRefundRequestAsync(new CreateBookingDepositRefundRequest
        {
            BookingDepositId = bookingDepositId,
            Amount = amount,
            Reason = BookingDepositRefundReasons.CustomerCancelledInTime,
            RequestedByUserId = requestedByUserId,
            IdempotencyKey = $"booking-cancel:{bookingDepositId}:{deposit.BookingId}"
        }, ct);
    }

    public async Task<BookingDepositRefundDto?> CreateVenueFaultRefundAsync(long bookingDepositId, long? requestedByUserId, CancellationToken ct)
    {
        var deposit = await db.BookingDeposits.AsNoTracking().FirstOrDefaultAsync(x => x.BookingDepositId == bookingDepositId, ct)
            ?? throw new NotFoundException("Booking deposit not found.");
        var amount = await CalculateRefundableBalanceAsync(bookingDepositId, ct);
        if (amount <= 0) return null;
        return await CreateRefundRequestAsync(new CreateBookingDepositRefundRequest
        {
            BookingDepositId = bookingDepositId,
            Amount = amount,
            Reason = BookingDepositRefundReasons.VenueFault,
            RequestedByUserId = requestedByUserId,
            IdempotencyKey = $"venue-fault:{bookingDepositId}:{deposit.BookingId}"
        }, ct);
    }

    public async Task<BookingDepositRefundDto?> CreateDepositExcessRefundAsync(long bookingDepositId, long invoiceId, decimal amount, CancellationToken ct)
    {
        if (amount <= 0) return null;
        return await CreateRefundRequestAsync(new CreateBookingDepositRefundRequest
        {
            BookingDepositId = bookingDepositId,
            Amount = amount,
            InvoiceId = invoiceId,
            Reason = BookingDepositRefundReasons.DepositExcess,
            IdempotencyKey = $"deposit-excess:{bookingDepositId}:{invoiceId}"
        }, ct);
    }

    public async Task<BookingDepositRefundDto> ApproveAsync(long refundId, long approvedByUserId, CancellationToken ct)
    {
        var refund = await GetRefundAsync(refundId, ct);
        EnsureTransition(refund.Status, BookingDepositRefundStatuses.Approved);
        refund.Status = BookingDepositRefundStatuses.Approved;
        refund.ApprovedByUserId = approvedByUserId;
        refund.ApprovedAtUtc = _clock.UtcNow;
        await db.SaveChangesAsync(ct);
        await LogAsync(approvedByUserId, AuditActions.BookingDepositRefundApproved, refund, ct);
        return Map(refund);
    }

    public async Task<BookingDepositRefundDto> RejectAsync(long refundId, long rejectedByUserId, string reason, CancellationToken ct)
    {
        var refund = await GetRefundAsync(refundId, ct);
        EnsureTransition(refund.Status, BookingDepositRefundStatuses.Rejected);
        refund.Status = BookingDepositRefundStatuses.Rejected;
        refund.RejectReason = reason.Trim();
        refund.RejectedAtUtc = _clock.UtcNow;
        await db.SaveChangesAsync(ct);
        await LogAsync(rejectedByUserId, AuditActions.BookingDepositRefundRejected, refund, ct);
        return Map(refund);
    }

    public async Task<BookingDepositRefundDto> MarkProcessingAsync(long refundId, long processedByUserId, CancellationToken ct)
    {
        var refund = await GetRefundAsync(refundId, ct);
        EnsureTransition(refund.Status, BookingDepositRefundStatuses.Processing);
        refund.Status = BookingDepositRefundStatuses.Processing;
        refund.ProcessedByUserId = processedByUserId;
        refund.ProcessingAtUtc = _clock.UtcNow;
        await db.SaveChangesAsync(ct);
        await LogAsync(processedByUserId, AuditActions.BookingDepositRefundProcessing, refund, ct);
        return Map(refund);
    }

    public async Task<BookingDepositRefundDto> MarkFailedAsync(long refundId, long processedByUserId, string reason, CancellationToken ct)
    {
        var refund = await GetRefundAsync(refundId, ct);
        EnsureTransition(refund.Status, BookingDepositRefundStatuses.Failed);
        refund.Status = BookingDepositRefundStatuses.Failed;
        refund.ProcessedByUserId = processedByUserId;
        refund.FailureReason = reason.Trim();
        refund.FailedAtUtc = _clock.UtcNow;
        await db.SaveChangesAsync(ct);
        await LogAsync(processedByUserId, AuditActions.BookingDepositRefundFailed, refund, ct);
        return Map(refund);
    }

    public async Task<BookingDepositRefundDto> CompleteAsync(long refundId, long processedByUserId, string? transferCode, CancellationToken ct)
    {
        await using var transaction = await BeginTransactionIfSupportedAsync(ct);
        var refund = await GetRefundAsync(refundId, ct);
        if (refund.Status == BookingDepositRefundStatuses.Succeeded)
        {
            if (transaction is not null) await transaction.CommitAsync(ct);
            return Map(refund);
        }
        EnsureTransition(refund.Status, BookingDepositRefundStatuses.Succeeded);

        var deposit = await db.BookingDeposits.FirstOrDefaultAsync(x => x.BookingDepositId == refund.BookingDepositId, ct)
            ?? throw new NotFoundException("Booking deposit not found.");

        refund.Status = BookingDepositRefundStatuses.Succeeded;
        refund.ProcessedByUserId = processedByUserId;
        refund.ManualTransferCode = string.IsNullOrWhiteSpace(transferCode) ? refund.ManualTransferCode : transferCode.Trim();
        refund.SucceededAtUtc = _clock.UtcNow;
        deposit.RefundedAmount += refund.Amount;
        deposit.RefundedAtUtc = _clock.UtcNow;
        UpdateDepositRefundStatus(deposit);

        await db.SaveChangesAsync(ct);
        await LogAsync(processedByUserId, AuditActions.BookingDepositRefundCompleted, refund, ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return Map(refund);
    }

    public async Task<DepositApplicationResult> ApplyDepositToInvoiceAsync(EntitySession session, EntityInvoice invoice, CancellationToken ct)
    {
        if (!session.BookingId.HasValue) return new DepositApplicationResult();

        var deposit = await db.BookingDeposits.FirstOrDefaultAsync(x =>
            x.BookingId == session.BookingId.Value &&
            x.PaidAmount > 0 &&
            (x.Status == BookingDepositStatuses.Paid ||
             x.Status == BookingDepositStatuses.AppliedToInvoice ||
             x.Status == BookingDepositStatuses.PartiallyRefunded ||
             x.Status == BookingDepositStatuses.Refunded), ct);
        if (deposit is null) return new DepositApplicationResult();

        var paymentMethod = await EnsureDepositPaymentMethodAsync(ct);
        var existingPayment = await db.Payments.FirstOrDefaultAsync(x =>
            x.InvoiceId == invoice.InvoiceId &&
            x.PaymentMethodId == paymentMethod.PaymentMethodId &&
            x.PaymentStatus == PaymentStatuses.Completed, ct);

        var appliedAmount = existingPayment?.Amount ?? Math.Min(deposit.PaidAmount, invoice.GrandTotalAmount);
        appliedAmount = Math.Min(appliedAmount, invoice.GrandTotalAmount);

        var paymentCreated = existingPayment is null && appliedAmount > 0;
        if (paymentCreated)
        {
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
        }

        deposit.AppliedAmount = appliedAmount;
        deposit.AppliedToInvoiceId = invoice.InvoiceId;
        if (deposit.Status == BookingDepositStatuses.Paid)
        {
            deposit.Status = BookingDepositStatuses.AppliedToInvoice;
        }

        await db.SaveChangesAsync(ct);
        await RecalculateInvoicePaymentStatusAsync(invoice, ct);

        var excess = Math.Max(0, deposit.PaidAmount - appliedAmount);
        var excessRefund = excess > 0
            ? await CreateDepositExcessRefundAsync(deposit.BookingDepositId, invoice.InvoiceId, excess, ct)
            : null;

        return new DepositApplicationResult
        {
            AppliedAmount = appliedAmount,
            ExcessAmount = excess,
            PaymentCreated = paymentCreated,
            ExcessRefund = excessRefund
        };
    }

    private async Task RecalculateInvoicePaymentStatusAsync(EntityInvoice invoice, CancellationToken ct)
    {
        var completedPayments = await db.Payments
            .Where(x => x.InvoiceId == invoice.InvoiceId && x.PaymentStatus == PaymentStatuses.Completed)
            .SumAsync(x => x.Amount, ct);
        invoice.PaidAmount = Math.Min(completedPayments, invoice.GrandTotalAmount);
        if (invoice.PaidAmount >= invoice.GrandTotalAmount && invoice.GrandTotalAmount > 0)
        {
            invoice.PaymentStatus = InvoicePaymentStatuses.Paid;
            invoice.Status = 2;
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
        await db.SaveChangesAsync(ct);
    }

    private async Task<PaymentMethod> EnsureDepositPaymentMethodAsync(CancellationToken ct)
    {
        var paymentMethod = await db.PaymentMethods.FirstOrDefaultAsync(x => x.Code == "DEPOSIT", ct);
        if (paymentMethod is not null) return paymentMethod;

        paymentMethod = new PaymentMethod
        {
            Code = "DEPOSIT",
            Name = "Deposit Applied",
            Description = "System payment method used when applying booking deposits to invoices.",
            IsActive = true
        };
        db.PaymentMethods.Add(paymentMethod);
        await db.SaveChangesAsync(ct);
        return paymentMethod;
    }

    private async Task<BookingDepositRefund> GetRefundAsync(long refundId, CancellationToken ct) =>
        await db.BookingDepositRefunds.FirstOrDefaultAsync(x => x.BookingDepositRefundId == refundId, ct)
        ?? throw new NotFoundException("Booking deposit refund not found.");

    private static bool HasUsablePaidDeposit(BookingDeposit deposit) =>
        deposit.PaidAmount > 0 &&
        deposit.Status is BookingDepositStatuses.Paid or BookingDepositStatuses.AppliedToInvoice or BookingDepositStatuses.PartiallyRefunded or BookingDepositStatuses.Refunded;

    private static void EnsureTransition(int current, int target)
    {
        var valid = current switch
        {
            BookingDepositRefundStatuses.PendingCustomerInfo => target == BookingDepositRefundStatuses.PendingApproval,
            BookingDepositRefundStatuses.PendingApproval => target is BookingDepositRefundStatuses.Approved or BookingDepositRefundStatuses.Rejected,
            BookingDepositRefundStatuses.Approved => target is BookingDepositRefundStatuses.Processing or BookingDepositRefundStatuses.ReadyForCashPickup or BookingDepositRefundStatuses.Cancelled,
            BookingDepositRefundStatuses.Processing => target is BookingDepositRefundStatuses.Succeeded or BookingDepositRefundStatuses.Failed,
            BookingDepositRefundStatuses.ReadyForCashPickup => target is BookingDepositRefundStatuses.Succeeded or BookingDepositRefundStatuses.Cancelled,
            _ => false
        };
        if (!valid) throw new BusinessRuleException("Invalid booking deposit refund status transition.");
    }

    private static void UpdateDepositRefundStatus(BookingDeposit deposit)
    {
        if (deposit.RefundedAmount <= 0) return;
        var consumed = deposit.AppliedAmount + deposit.ForfeitedAmount + deposit.RefundedAmount;
        deposit.Status = consumed >= deposit.PaidAmount && deposit.AppliedAmount <= 0 && deposit.ForfeitedAmount <= 0
            ? BookingDepositStatuses.Refunded
            : BookingDepositStatuses.PartiallyRefunded;
    }

    private async Task<string> NextRefundCodeAsync(CancellationToken ct)
    {
        for (var i = 0; i < 5; i++)
        {
            var code = $"RF{_clock.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";
            if (!await db.BookingDepositRefunds.AnyAsync(x => x.RefundCode == code, ct)) return code;
        }
        return $"RF{Guid.NewGuid():N}"[..24].ToUpperInvariant();
    }

    private Task LogAsync(long? actorUserId, string action, BookingDepositRefund refund, CancellationToken ct) =>
        auditService?.LogAsync(actorUserId, action, nameof(BookingDepositRefund), refund.BookingDepositRefundId, refund.PublicId, newValues: refund, ct: ct)
        ?? Task.CompletedTask;

    private static BookingDepositRefundDto Map(BookingDepositRefund refund) => new()
    {
        BookingDepositRefundId = refund.BookingDepositRefundId,
        PublicId = refund.PublicId,
        BookingDepositId = refund.BookingDepositId,
        BookingId = refund.BookingId,
        InvoiceId = refund.InvoiceId,
        CustomerId = refund.CustomerId,
        RefundCode = refund.RefundCode,
        Amount = refund.Amount,
        Status = refund.Status,
        Reason = refund.Reason,
        RefundMethod = refund.RefundMethod,
        ReasonDetail = refund.ReasonDetail,
        IdempotencyKey = refund.IdempotencyKey,
        CreatedAtUtc = refund.CreatedAtUtc
    };

    private async Task<IDbContextTransaction?> BeginTransactionIfSupportedAsync(CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is not null) return null;
        try
        {
            return await db.Database.BeginTransactionAsync(ct);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}
