using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using PoolHub.Core.DTOs.BookingDepositRefund;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Exceptions;
using PoolHub.Shared.Time;
using EntitySession = PoolHub.Core.Entities.Session;
using EntityInvoice = PoolHub.Core.Entities.Invoice;
using EntityCustomer = PoolHub.Core.Entities.Customer;
using EntityBooking = PoolHub.Core.Entities.Booking;

namespace PoolHub.Services.Booking;

public class BookingDepositRefundService(
    PoolHubDbContext db,
    IAuditService? auditService = null,
    IClock? clock = null,
    IEmailService? emailService = null,
    IConfiguration? config = null,
    ISensitiveDataProtector? sensitiveDataProtector = null,
    ILogger<BookingDepositRefundService>? logger = null) : IBookingDepositRefundService
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

    private static readonly int[] PendingRefundStatuses =
    [
        BookingDepositRefundStatuses.PendingCustomerInfo,
        BookingDepositRefundStatuses.PendingApproval,
        BookingDepositRefundStatuses.Approved,
        BookingDepositRefundStatuses.Processing,
        BookingDepositRefundStatuses.ReadyForCashPickup
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

    public async Task<DepositRefundSummaryDto?> GetSummaryForBookingAsync(long bookingId, CancellationToken ct)
    {
        var deposit = await db.BookingDeposits.AsNoTracking()
            .Where(x => x.BookingId == bookingId)
            .OrderByDescending(x => x.BookingDepositId)
            .FirstOrDefaultAsync(ct);

        return deposit is null ? null : await BuildSummaryAsync(deposit, ct);
    }

    public async Task<DepositRefundSummaryDto?> GetSummaryForInvoiceAsync(long invoiceId, CancellationToken ct)
    {
        var deposit = await db.BookingDeposits.AsNoTracking()
            .Where(x => x.AppliedToInvoiceId == invoiceId)
            .OrderByDescending(x => x.BookingDepositId)
            .FirstOrDefaultAsync(ct);

        return deposit is null ? null : await BuildSummaryAsync(deposit, ct);
    }

    public async Task<DepositRefundSummaryDto?> GetSummaryForSessionAsync(long sessionId, CancellationToken ct)
    {
        var session = await db.Sessions.AsNoTracking()
            .Where(x => x.SessionId == sessionId)
            .Select(x => new { x.BookingId })
            .FirstOrDefaultAsync(ct);

        if (session?.BookingId is null) return null;
        return await GetSummaryForBookingAsync(session.BookingId.Value, ct);
    }

    private async Task<DepositRefundSummaryDto> BuildSummaryAsync(BookingDeposit deposit, CancellationToken ct)
    {
        var refundRows = await db.BookingDepositRefunds.AsNoTracking()
            .Where(x => x.BookingDepositId == deposit.BookingDepositId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new DepositRefundRequestSummaryDto
            {
                BookingDepositRefundId = x.BookingDepositRefundId,
                PublicId = x.PublicId,
                RefundCode = x.RefundCode,
                Amount = x.Amount,
                Reason = x.Reason,
                RefundMethod = x.RefundMethod,
                Status = x.Status,
                CreatedAtUtc = x.CreatedAtUtc,
                SucceededAtUtc = x.SucceededAtUtc
            })
            .ToListAsync(ct);

        var pendingRefundAmount = refundRows
            .Where(x => PendingRefundStatuses.Contains(x.Status))
            .Sum(x => x.Amount);

        var refundableBalance = Math.Max(
            0,
            deposit.PaidAmount
            - deposit.AppliedAmount
            - deposit.ForfeitedAmount
            - pendingRefundAmount
            - deposit.RefundedAmount);

        return new DepositRefundSummaryDto
        {
            PaidAmount = deposit.PaidAmount,
            AppliedAmount = deposit.AppliedAmount,
            ForfeitedAmount = deposit.ForfeitedAmount,
            PendingRefundAmount = pendingRefundAmount,
            RefundedAmount = deposit.RefundedAmount,
            RefundableBalance = refundableBalance,
            RefundRequests = refundRows
        };
    }

    private async Task<decimal> CalculateRefundableBalanceExcludingRefundAsync(long bookingDepositId, long refundId, CancellationToken ct)
    {
        var deposit = await db.BookingDeposits.AsNoTracking().FirstOrDefaultAsync(x => x.BookingDepositId == bookingDepositId, ct)
            ?? throw new NotFoundException("Booking deposit not found.");
        var reserved = await db.BookingDepositRefunds.AsNoTracking()
            .Where(x => x.BookingDepositRefundId != refundId &&
                        x.BookingDepositId == bookingDepositId &&
                        BalanceHoldingStatuses.Contains(x.Status))
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
        var rawToken = GenerateToken();
        SetCustomerToken(refund, rawToken);
        db.BookingDepositRefunds.Add(refund);
        await db.SaveChangesAsync(ct);
        await LogAsync(request.RequestedByUserId, AuditActions.BookingDepositRefundCreated, refund, ct);
        await LogAsync(request.RequestedByUserId, AuditActions.RefundTokenGenerated, refund, ct);
        await TrySendRefundEmailAsync(refund, "Yeu cau thong tin hoan coc", "Cung cap thong tin nhan hoan coc",
            "PoolHub can ban xac minh va chon hinh thuc nhan tien hoan coc.",
            BuildPublicRefundUrl(rawToken), "Mo trang hoan coc", ct);
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
        if (refund.RefundMethod is null) throw new BusinessRuleException("Refund method must be selected before approval.");
        if (refund.RefundMethod == BookingDepositRefundMethods.BankTransfer &&
            (string.IsNullOrWhiteSpace(refund.CustomerBankAccountNumberEncrypted) || string.IsNullOrWhiteSpace(refund.CustomerBankAccountNameEncrypted)))
            throw new BusinessRuleException("Bank information is required before approval.");
        var balance = await CalculateRefundableBalanceExcludingRefundAsync(refund.BookingDepositId, refund.BookingDepositRefundId, ct);
        if (refund.Amount > balance)
            throw new BusinessRuleException("Refund amount exceeds the current refundable balance.");
        refund.Status = BookingDepositRefundStatuses.Approved;
        refund.ApprovedByUserId = approvedByUserId;
        refund.ApprovedAtUtc = _clock.UtcNow;
        await db.SaveChangesAsync(ct);
        await LogAsync(approvedByUserId, AuditActions.BookingDepositRefundApproved, refund, ct);
        await TrySendRefundEmailAsync(refund, "Hoan coc da duoc duyet", "Hoan coc da duoc duyet",
            "Yeu cau hoan coc cua ban da duoc quan ly duyet va dang cho thu ngan xu ly.", null, null, ct);
        return Map(refund);
    }

    public async Task<BookingDepositRefundDto> RejectAsync(long refundId, long rejectedByUserId, string reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ValidationException("Reject reason is required.");
        var refund = await GetRefundAsync(refundId, ct);
        EnsureTransition(refund.Status, BookingDepositRefundStatuses.Rejected);
        refund.Status = BookingDepositRefundStatuses.Rejected;
        refund.RejectReason = reason.Trim();
        refund.RejectedAtUtc = _clock.UtcNow;
        await db.SaveChangesAsync(ct);
        await LogAsync(rejectedByUserId, AuditActions.BookingDepositRefundRejected, refund, ct);
        await TrySendRefundEmailAsync(refund, "Yeu cau hoan coc bi tu choi", "Yeu cau hoan coc bi tu choi",
            $"Ly do: {refund.RejectReason}", null, null, ct);
        return Map(refund);
    }

    public async Task<BookingDepositRefundDto> MarkProcessingAsync(long refundId, long processedByUserId, CancellationToken ct)
    {
        var refund = await GetRefundAsync(refundId, ct);
        if (refund.RefundMethod != BookingDepositRefundMethods.BankTransfer)
            throw new BusinessRuleException("Only BankTransfer refunds can be marked Processing.");
        EnsureTransition(refund.Status, BookingDepositRefundStatuses.Processing);
        refund.Status = BookingDepositRefundStatuses.Processing;
        refund.ProcessedByUserId = processedByUserId;
        refund.ProcessingAtUtc = _clock.UtcNow;
        await db.SaveChangesAsync(ct);
        await LogAsync(processedByUserId, AuditActions.BookingDepositRefundProcessing, refund, ct);
        await TrySendRefundEmailAsync(refund, "Hoan coc dang xu ly", "Hoan coc dang xu ly",
            "Thu ngan dang thuc hien chuyen khoan hoan coc.", null, null, ct);
        return Map(refund);
    }

    public async Task<BookingDepositRefundDto> MarkFailedAsync(long refundId, long processedByUserId, string reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ValidationException("Failure reason is required.");
        var refund = await GetRefundAsync(refundId, ct);
        EnsureTransition(refund.Status, BookingDepositRefundStatuses.Failed);
        refund.Status = BookingDepositRefundStatuses.Failed;
        refund.ProcessedByUserId = processedByUserId;
        refund.FailureReason = reason.Trim();
        refund.FailedAtUtc = _clock.UtcNow;
        await db.SaveChangesAsync(ct);
        await LogAsync(processedByUserId, AuditActions.BookingDepositRefundFailed, refund, ct);
        await TrySendRefundEmailAsync(refund, "Hoan coc that bai", "Hoan coc that bai",
            $"Ly do: {refund.FailureReason}", null, null, ct);
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
        var balance = await CalculateRefundableBalanceExcludingRefundAsync(refund.BookingDepositId, refund.BookingDepositRefundId, ct);
        if (refund.Amount > balance)
        {
            throw new BusinessRuleException("Refund amount exceeds remaining refundable balance.");
        }

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

    public async Task<PublicDepositRefundDto> GetPublicAsync(string token, CancellationToken ct)
    {
        var refund = await GetRefundByTokenAsync(token, ct);
        return await MapPublicAsync(refund, ct);
    }

    public async Task SendVerificationCodeAsync(string token, CancellationToken ct)
    {
        var refund = await GetRefundByTokenAsync(token, ct);
        EnsurePendingCustomerInfo(refund);
        if (refund.VerificationCodeSentAtUtc.HasValue &&
            refund.VerificationCodeSentAtUtc.Value > _clock.UtcNow.AddSeconds(-GetVerificationResendSeconds()))
            throw new BusinessRuleException("Please wait before requesting another verification code.");

        var code = GenerateNumericCode();
        refund.VerificationCodeHash = HashToken(code);
        refund.VerificationCodeExpiresAtUtc = _clock.UtcNow.AddMinutes(GetVerificationMinutes());
        refund.VerificationCodeSentAtUtc = _clock.UtcNow;
        refund.VerificationFailedAttempts = 0;
        refund.CustomerVerifiedAtUtc = null;
        await db.SaveChangesAsync(ct);
        await LogAsync(null, AuditActions.RefundVerificationCodeSent, refund, ct);
        await TrySendRefundEmailAsync(refund, "Ma xac minh hoan coc", "Ma xac minh hoan coc",
            $"Ma xac minh cua ban la {code}. Ma het han sau {GetVerificationMinutes()} phut.",
            null, null, ct);
    }

    public async Task<PublicDepositRefundDto> VerifyCustomerAsync(string token, VerifyDepositRefundRequest request, CancellationToken ct)
    {
        var refund = await GetRefundByTokenAsync(token, ct);
        EnsurePendingCustomerInfo(refund);
        if (refund.VerificationCodeHash is null || refund.VerificationCodeExpiresAtUtc <= _clock.UtcNow)
            throw new UnauthorizedException("Verification code is invalid or expired.");
        if (refund.VerificationFailedAttempts >= GetVerificationMaxAttempts())
            throw new UnauthorizedException("Too many invalid verification attempts.");
        if (!PhoneLast4Matches(refund.CustomerPhoneSnapshot, request.PhoneLast4) ||
            !FixedTimeEquals(refund.VerificationCodeHash, HashToken(request.VerificationCode.Trim())))
        {
            refund.VerificationFailedAttempts += 1;
            await db.SaveChangesAsync(ct);
            throw new UnauthorizedException("Verification code or phone last 4 digits are incorrect.");
        }

        refund.CustomerVerifiedAtUtc = _clock.UtcNow;
        refund.VerificationFailedAttempts = 0;
        await db.SaveChangesAsync(ct);
        await LogAsync(null, AuditActions.RefundCustomerVerified, refund, ct);
        return await MapPublicAsync(refund, ct);
    }

    public async Task<PublicDepositRefundDto> SubmitMethodAsync(string token, SubmitDepositRefundMethodRequest request, CancellationToken ct)
    {
        var refund = await GetRefundByTokenAsync(token, ct);
        EnsurePendingCustomerInfo(refund);
        EnsureCustomerVerified(refund);
        var method = ParseRefundMethod(request.RefundMethod);

        if (method == BookingDepositRefundMethods.BankTransfer)
        {
            if (sensitiveDataProtector is null) throw new ServiceUnavailableException("Sensitive data protection is not configured.");
            var accountNumber = NormalizeRequired(request.AccountNumber, "Account number is required.");
            var confirm = NormalizeRequired(request.ConfirmAccountNumber, "Confirm account number is required.");
            if (!string.Equals(accountNumber, confirm, StringComparison.Ordinal))
                throw new ValidationException("Account numbers do not match.");
            refund.CustomerBankCode = NormalizeRequired(request.BankCode, "Bank code is required.").ToUpperInvariant();
            refund.CustomerBankName = NormalizeRequired(request.BankName, "Bank name is required.");
            refund.CustomerBankAccountNumberEncrypted = sensitiveDataProtector.Protect(accountNumber);
            refund.CustomerBankAccountNameEncrypted = sensitiveDataProtector.Protect(NormalizeHolderName(request.AccountHolderName));
            refund.CustomerBankAccountLast4 = Last4(accountNumber);
        }
        else
        {
            refund.CustomerBankCode = null;
            refund.CustomerBankName = null;
            refund.CustomerBankAccountNumberEncrypted = null;
            refund.CustomerBankAccountNameEncrypted = null;
            refund.CustomerBankAccountLast4 = null;
        }

        refund.RefundMethod = method;
        refund.Status = BookingDepositRefundStatuses.PendingApproval;
        refund.CustomerInfoSubmittedAtUtc = _clock.UtcNow;
        await db.SaveChangesAsync(ct);
        await LogAsync(null, AuditActions.CustomerRefundInfoSubmitted, refund, ct);
        await TrySendRefundEmailAsync(refund, "Da nhan thong tin hoan coc", "Da nhan thong tin hoan coc",
            "Thong tin nhan hoan coc cua ban da duoc gui den quan ly de duyet.", null, null, ct);
        return await MapPublicAsync(refund, ct);
    }

    public async Task<PagedResult<DepositRefundManagementDto>> GetRefundsAsync(DepositRefundQueryRequest request, CancellationToken ct)
    {
        request.PageNumber = Math.Max(1, request.PageNumber);
        request.PageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = ManagementQuery();
        if (request.Status.HasValue) query = query.Where(x => x.Refund.Status == request.Status.Value);
        if (request.RefundMethod.HasValue) query = query.Where(x => x.Refund.RefundMethod == request.RefundMethod.Value);
        if (!string.IsNullOrWhiteSpace(request.Reason)) query = query.Where(x => x.Refund.Reason == request.Reason.Trim());
        if (!string.IsNullOrWhiteSpace(request.BookingCode)) query = query.Where(x => x.Booking.BookingCode.Contains(request.BookingCode.Trim()));
        if (!string.IsNullOrWhiteSpace(request.RefundCode)) query = query.Where(x => x.Refund.RefundCode.Contains(request.RefundCode.Trim()));
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.Refund.RefundCode.Contains(search) || x.Booking.BookingCode.Contains(search) ||
                (x.Customer != null && (x.Customer.FullName.Contains(search) || x.Customer.PhoneNumber.Contains(search) || (x.Customer.Email != null && x.Customer.Email.Contains(search)))));
        }
        if (request.CreatedFromUtc.HasValue) query = query.Where(x => x.Refund.CreatedAtUtc >= request.CreatedFromUtc.Value);
        if (request.CreatedToUtc.HasValue) query = query.Where(x => x.Refund.CreatedAtUtc <= request.CreatedToUtc.Value);

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.Refund.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);
        return new PagedResult<DepositRefundManagementDto>
        {
            Items = rows.Select(x => MapManagement(x.Refund, x.Booking, x.Customer)).ToList(),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalItems = total
        };
    }

    public async Task<DepositRefundManagementDto> GetManagementAsync(long refundId, CancellationToken ct)
    {
        var row = await ManagementQuery().FirstOrDefaultAsync(x => x.Refund.BookingDepositRefundId == refundId, ct)
            ?? throw new NotFoundException("Booking deposit refund not found.");
        return MapManagement(row.Refund, row.Booking, row.Customer);
    }

    public async Task<DepositRefundBankInfoDto> GetBankInfoAsync(long refundId, long actorUserId, CancellationToken ct)
    {
        var refund = await GetRefundAsync(refundId, ct);
        if (refund.RefundMethod != BookingDepositRefundMethods.BankTransfer)
            throw new BusinessRuleException("Refund is not configured for bank transfer.");
        if (sensitiveDataProtector is null) throw new ServiceUnavailableException("Sensitive data protection is not configured.");
        await LogAsync(actorUserId, AuditActions.BankInformationViewed, refund, ct);
        return new DepositRefundBankInfoDto
        {
            BookingDepositRefundId = refund.BookingDepositRefundId,
            RefundCode = refund.RefundCode,
            BankCode = refund.CustomerBankCode,
            BankName = refund.CustomerBankName,
            AccountNumber = refund.CustomerBankAccountNumberEncrypted is null ? null : sensitiveDataProtector.Unprotect(refund.CustomerBankAccountNumberEncrypted),
            AccountHolderName = refund.CustomerBankAccountNameEncrypted is null ? null : sensitiveDataProtector.Unprotect(refund.CustomerBankAccountNameEncrypted),
            AccountLast4 = refund.CustomerBankAccountLast4
        };
    }

    public async Task<BookingDepositRefundDto> RequestCustomerUpdateAsync(long refundId, long actorUserId, string? reason, CancellationToken ct)
    {
        var refund = await GetRefundAsync(refundId, ct);
        EnsureTransition(refund.Status, BookingDepositRefundStatuses.PendingCustomerInfo);
        refund.Status = BookingDepositRefundStatuses.PendingCustomerInfo;
        refund.RefundMethod = null;
        refund.CustomerVerifiedAtUtc = null;
        refund.VerificationCodeHash = null;
        refund.VerificationCodeExpiresAtUtc = null;
        refund.VerificationCodeSentAtUtc = null;
        refund.VerificationFailedAttempts = 0;
        refund.CustomerInfoSubmittedAtUtc = null;
        refund.CustomerBankCode = null;
        refund.CustomerBankName = null;
        refund.CustomerBankAccountNumberEncrypted = null;
        refund.CustomerBankAccountNameEncrypted = null;
        refund.CustomerBankAccountLast4 = null;
        refund.Note = string.IsNullOrWhiteSpace(reason) ? refund.Note : reason.Trim();
        var rawToken = GenerateToken();
        SetCustomerToken(refund, rawToken);
        await db.SaveChangesAsync(ct);
        await LogAsync(actorUserId, AuditActions.DepositRefundCustomerUpdateRequested, refund, ct);
        await TrySendRefundEmailAsync(refund, "Can cap nhat thong tin hoan coc", "Can cap nhat thong tin hoan coc",
            "Quan ly can ban cap nhat lai thong tin nhan hoan coc.", BuildPublicRefundUrl(rawToken), "Cap nhat thong tin", ct);
        return Map(refund);
    }

    public async Task<BookingDepositRefundDto> PrepareCashPickupAsync(long refundId, long processedByUserId, CancellationToken ct)
    {
        var refund = await GetRefundAsync(refundId, ct);
        if (refund.RefundMethod != BookingDepositRefundMethods.CashAtVenue)
            throw new BusinessRuleException("Only CashAtVenue refunds can be prepared for cash pickup.");
        EnsureTransition(refund.Status, BookingDepositRefundStatuses.ReadyForCashPickup);
        var code = GenerateNumericCode();
        refund.CashPickupCodeHash = HashToken(code);
        refund.CashPickupCodeExpiresAtUtc = _clock.UtcNow.AddHours(GetCashPickupCodeHours());
        refund.CashPickupCodeUsedAtUtc = null;
        refund.CashReceiptCode = $"CASHRF{_clock.UtcNow:yyyyMMddHHmmss}";
        refund.Status = BookingDepositRefundStatuses.ReadyForCashPickup;
        refund.ProcessedByUserId = processedByUserId;
        await db.SaveChangesAsync(ct);
        await LogAsync(processedByUserId, AuditActions.CashRefundPrepared, refund, ct);
        await TrySendRefundEmailAsync(refund, "Tien mat san sang nhan", "Tien mat san sang nhan",
            $"Ma nhan tien mat cua ban la {code}. Vui long mang ma nay den quay thu ngan.",
            null, null, ct);
        return Map(refund);
    }

    public async Task<BookingDepositRefundDto> CompleteBankTransferAsync(long refundId, long processedByUserId, CompleteBankTransferRefundRequest request, CancellationToken ct)
    {
        var refund = await GetRefundAsync(refundId, ct);
        if (refund.RefundMethod != BookingDepositRefundMethods.BankTransfer)
            throw new BusinessRuleException("Refund method must be BankTransfer.");
        if (refund.Status != BookingDepositRefundStatuses.Processing)
            throw new BusinessRuleException("Bank transfer refund must be Processing before completion.");
        var transferCode = NormalizeRequired(request.ManualTransferCode, "Manual transfer code is required.");
        refund.ProofMediaAssetId = request.ProofMediaAssetId;
        refund.Note = NormalizeOptional(request.Note);
        var result = await CompleteAsync(refundId, processedByUserId, transferCode, ct);
        await TrySendRefundEmailAsync(refund, "Da chuyen khoan hoan coc", "Da chuyen khoan hoan coc",
            "PoolHub da hoan coc bang chuyen khoan ngan hang.", null, null, ct);
        return result;
    }

    public async Task<BookingDepositRefundDto> CompleteCashPickupAsync(long refundId, long processedByUserId, CompleteCashPickupRefundRequest request, CancellationToken ct)
    {
        var refund = await GetRefundAsync(refundId, ct);
        if (refund.RefundMethod != BookingDepositRefundMethods.CashAtVenue)
            throw new BusinessRuleException("Refund method must be CashAtVenue.");
        if (refund.Status != BookingDepositRefundStatuses.ReadyForCashPickup)
            throw new BusinessRuleException("Cash refund is not ready for pickup.");
        if (refund.CashPickupCodeUsedAtUtc.HasValue)
            throw new ConflictException("Cash pickup code has already been used.");
        if (refund.CashPickupCodeHash is null || refund.CashPickupCodeExpiresAtUtc <= _clock.UtcNow ||
            !FixedTimeEquals(refund.CashPickupCodeHash, HashToken(request.CashPickupCode.Trim())))
            throw new UnauthorizedException("Cash pickup code is invalid or expired.");
        var booking = await db.Bookings.AsNoTracking().FirstOrDefaultAsync(x => x.BookingId == refund.BookingId, ct)
            ?? throw new NotFoundException("Booking not found.");
        if (!string.Equals(booking.BookingCode, request.BookingCode.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedException("Booking code does not match.");
        if (!PhoneLast4Matches(refund.CustomerPhoneSnapshot, request.PhoneLast4))
            throw new UnauthorizedException("Phone last 4 digits do not match.");

        refund.CashPickupCodeUsedAtUtc = _clock.UtcNow;
        refund.Note = NormalizeOptional(request.Note);
        var result = await CompleteAsync(refundId, processedByUserId, refund.CashReceiptCode, ct);
        await LogAsync(processedByUserId, AuditActions.CashRefundPickedUp, refund, ct);
        await TrySendRefundEmailAsync(refund, "Da hoan tien mat", "Da hoan tien mat",
            "PoolHub da xac nhan ban da nhan tien mat hoan coc.", null, null, ct);
        return result;
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

    private async Task<BookingDepositRefund> GetRefundByTokenAsync(string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token)) throw new NotFoundException("Deposit refund not found.");
        var hash = HashToken(token.Trim());
        var refund = await db.BookingDepositRefunds.FirstOrDefaultAsync(x => x.CustomerTokenHash == hash, ct)
            ?? throw new NotFoundException("Deposit refund not found.");
        if (refund.CustomerTokenExpiresAtUtc <= _clock.UtcNow)
            throw new UnauthorizedException("Deposit refund link is invalid or expired.");
        if (refund.Status is BookingDepositRefundStatuses.Succeeded or BookingDepositRefundStatuses.Rejected or BookingDepositRefundStatuses.Cancelled)
            throw new BusinessRuleException("Deposit refund is already closed.");
        return refund;
    }

    private IQueryable<RefundManagementRow> ManagementQuery() =>
        from refund in db.BookingDepositRefunds.AsNoTracking()
        join booking in db.Bookings.AsNoTracking() on refund.BookingId equals booking.BookingId
        join customer in db.Customers.AsNoTracking() on refund.CustomerId equals customer.CustomerId into customers
        from customer in customers.DefaultIfEmpty()
        select new RefundManagementRow(refund, booking, customer);

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

    private static void EnsurePendingCustomerInfo(BookingDepositRefund refund)
    {
        if (refund.Status != BookingDepositRefundStatuses.PendingCustomerInfo)
            throw new BusinessRuleException("Deposit refund is not waiting for customer information.");
    }

    private void EnsureCustomerVerified(BookingDepositRefund refund)
    {
        if (!refund.CustomerVerifiedAtUtc.HasValue ||
            refund.CustomerVerifiedAtUtc.Value < _clock.UtcNow.AddMinutes(-GetVerifiedSessionMinutes()))
            throw new UnauthorizedException("Customer verification is required or expired.");
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

    private void SetCustomerToken(BookingDepositRefund refund, string rawToken)
    {
        refund.CustomerTokenHash = HashToken(rawToken);
        refund.CustomerTokenGeneratedAtUtc = _clock.UtcNow;
        refund.CustomerTokenExpiresAtUtc = _clock.UtcNow.AddHours(GetTokenHours());
    }

    private async Task<PublicDepositRefundDto> MapPublicAsync(BookingDepositRefund refund, CancellationToken ct)
    {
        var bookingCode = await db.Bookings.AsNoTracking()
            .Where(x => x.BookingId == refund.BookingId)
            .Select(x => x.BookingCode)
            .FirstOrDefaultAsync(ct);
        return new PublicDepositRefundDto
        {
            RefundCode = refund.RefundCode,
            BookingCode = bookingCode,
            Amount = refund.Amount,
            Reason = refund.Reason,
            Status = refund.Status,
            TokenExpiresAtUtc = refund.CustomerTokenExpiresAtUtc,
            CustomerEmailMasked = MaskEmail(refund.CustomerEmailSnapshot),
            CustomerPhoneMasked = MaskPhone(refund.CustomerPhoneSnapshot),
            RefundMethod = refund.RefundMethod,
            BankCode = refund.CustomerBankCode,
            BankName = refund.CustomerBankName,
            BankAccountLast4 = refund.CustomerBankAccountLast4,
            IsVerified = refund.CustomerVerifiedAtUtc.HasValue && refund.CustomerVerifiedAtUtc.Value >= _clock.UtcNow.AddMinutes(-GetVerifiedSessionMinutes()),
            NextStep = GetPublicNextStep(refund)
        };
    }

    private static DepositRefundManagementDto MapManagement(BookingDepositRefund refund, EntityBooking booking, EntityCustomer? customer)
    {
        var dto = new DepositRefundManagementDto
        {
            BookingCode = booking.BookingCode,
            CustomerName = customer?.FullName,
            CustomerEmailMasked = MaskEmail(refund.CustomerEmailSnapshot ?? customer?.Email),
            CustomerPhoneMasked = MaskPhone(refund.CustomerPhoneSnapshot ?? customer?.PhoneNumber),
            BankCode = refund.CustomerBankCode,
            BankName = refund.CustomerBankName,
            BankAccountLast4 = refund.CustomerBankAccountLast4,
            ManualTransferCode = refund.ManualTransferCode,
            FailureReason = refund.FailureReason,
            RejectReason = refund.RejectReason,
            Note = refund.Note,
            ApprovedAtUtc = refund.ApprovedAtUtc,
            ProcessingAtUtc = refund.ProcessingAtUtc,
            SucceededAtUtc = refund.SucceededAtUtc
        };
        var baseDto = Map(refund);
        dto.BookingDepositRefundId = baseDto.BookingDepositRefundId;
        dto.PublicId = baseDto.PublicId;
        dto.BookingDepositId = baseDto.BookingDepositId;
        dto.BookingId = baseDto.BookingId;
        dto.InvoiceId = baseDto.InvoiceId;
        dto.CustomerId = baseDto.CustomerId;
        dto.RefundCode = baseDto.RefundCode;
        dto.Amount = baseDto.Amount;
        dto.Status = baseDto.Status;
        dto.Reason = baseDto.Reason;
        dto.RefundMethod = baseDto.RefundMethod;
        dto.ReasonDetail = baseDto.ReasonDetail;
        dto.IdempotencyKey = baseDto.IdempotencyKey;
        dto.CreatedAtUtc = baseDto.CreatedAtUtc;
        return dto;
    }

    private async Task TrySendRefundEmailAsync(BookingDepositRefund refund, string subject, string title, string message, string? actionUrl, string? actionText, CancellationToken ct)
    {
        if (emailService is null || string.IsNullOrWhiteSpace(refund.CustomerEmailSnapshot)) return;
        try
        {
            var bookingCode = await db.Bookings.AsNoTracking()
                .Where(x => x.BookingId == refund.BookingId)
                .Select(x => x.BookingCode)
                .FirstOrDefaultAsync(ct) ?? refund.BookingId.ToString();
            await emailService.SendDepositRefundNotificationAsync(refund.CustomerEmailSnapshot, subject, title, message,
                new Dictionary<string, string>
                {
                    ["Ma hoan coc"] = refund.RefundCode,
                    ["Ma booking"] = bookingCode,
                    ["So tien"] = $"{refund.Amount:N0} VND",
                    ["Ly do"] = refund.Reason
                },
                actionUrl, actionText, ct);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to send deposit refund email for {RefundCode}", refund.RefundCode);
        }
    }

    private string BuildPublicRefundUrl(string token)
    {
        var baseUrl = config?["EmailSettings:FrontendBaseUrl"] ?? "http://localhost:3000";
        return $"{baseUrl.TrimEnd('/')}/deposit-refunds/{Uri.EscapeDataString(token)}";
    }

    private int GetTokenHours() => ReadInt("Refunds:CustomerTokenHours", 48, 1, 168);
    private int GetVerificationMinutes() => ReadInt("Refunds:VerificationCodeMinutes", 10, 1, 60);
    private int GetVerificationResendSeconds() => ReadInt("Refunds:VerificationResendSeconds", 60, 10, 600);
    private int GetVerificationMaxAttempts() => ReadInt("Refunds:VerificationMaxAttempts", 5, 1, 20);
    private int GetVerifiedSessionMinutes() => ReadInt("Refunds:VerifiedSessionMinutes", 30, 5, 240);
    private int GetCashPickupCodeHours() => ReadInt("Refunds:CashPickupCodeHours", 48, 1, 168);

    private int ReadInt(string key, int fallback, int min, int max) =>
        int.TryParse(config?[key], out var value) ? Math.Clamp(value, min, max) : fallback;

    private static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string GenerateNumericCode() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private static bool FixedTimeEquals(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(left), Encoding.UTF8.GetBytes(right));

    private static int ParseRefundMethod(string value) =>
        value.Trim().Equals("BankTransfer", StringComparison.OrdinalIgnoreCase) ? BookingDepositRefundMethods.BankTransfer :
        value.Trim().Equals("CashAtVenue", StringComparison.OrdinalIgnoreCase) ? BookingDepositRefundMethods.CashAtVenue :
        throw new ValidationException("Refund method must be BankTransfer or CashAtVenue.");

    private static string NormalizeRequired(string? value, string message) =>
        string.IsNullOrWhiteSpace(value) ? throw new ValidationException(message) : value.Trim();

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeHolderName(string? value) =>
        string.Join(' ', NormalizeRequired(value, "Account holder name is required.").Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();

    private static string Last4(string value) => value.Length <= 4 ? value : value[^4..];

    private static bool PhoneLast4Matches(string? phone, string? last4)
    {
        if (string.IsNullOrWhiteSpace(last4)) return false;
        var normalized = PhoneNumberNormalizer.Normalize(phone);
        return normalized.Length >= 4 && string.Equals(normalized[^4..], last4.Trim(), StringComparison.Ordinal);
    }

    private static string? MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var at = email.IndexOf('@');
        if (at <= 1) return $"***{email[at..]}";
        return $"{email[0]}***{email[(at - 1)..]}";
    }

    private static string? MaskPhone(string? phone)
    {
        var normalized = PhoneNumberNormalizer.Normalize(phone);
        if (normalized.Length < 4) return string.IsNullOrWhiteSpace(normalized) ? null : "***";
        return $"***{normalized[^4..]}";
    }

    private string GetPublicNextStep(BookingDepositRefund refund) => refund.Status switch
    {
        BookingDepositRefundStatuses.PendingCustomerInfo when refund.CustomerVerifiedAtUtc is null => "Verify email code and phone last 4 digits.",
        BookingDepositRefundStatuses.PendingCustomerInfo => "Choose refund method.",
        BookingDepositRefundStatuses.PendingApproval => "Waiting for manager approval.",
        BookingDepositRefundStatuses.Approved => "Waiting for cashier processing.",
        BookingDepositRefundStatuses.Processing => "Bank transfer is being processed.",
        BookingDepositRefundStatuses.ReadyForCashPickup => "Cash is ready for pickup at the venue.",
        BookingDepositRefundStatuses.Succeeded => "Refund completed.",
        BookingDepositRefundStatuses.Rejected => "Refund rejected.",
        BookingDepositRefundStatuses.Failed => "Refund failed.",
        _ => "No further action is available."
    };

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

    private sealed record RefundManagementRow(BookingDepositRefund Refund, EntityBooking Booking, EntityCustomer? Customer);
}
