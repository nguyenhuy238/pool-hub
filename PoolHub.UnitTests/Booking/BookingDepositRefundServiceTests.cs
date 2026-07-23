using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.BookingDepositRefund;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Services.Booking;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Exceptions;
using Xunit;

namespace PoolHub.UnitTests;

public class BookingDepositRefundServiceTests
{
    [Fact]
    public async Task CreateRefundRequestAsync_WhenAmountIsZero_Throws()
    {
        await using var db = CreateDb();
        SeedPaidDeposit(db, paidAmount: 100000);
        await db.SaveChangesAsync();

        var service = new BookingDepositRefundService(db);

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateRefundRequestAsync(new CreateBookingDepositRefundRequest
        {
            BookingDepositId = 1,
            Amount = 0,
            Reason = BookingDepositRefundReasons.Other,
            IdempotencyKey = "manual:1"
        }, CancellationToken.None));
    }

    [Fact]
    public async Task CreateRefundRequestAsync_WhenAmountExceedsBalance_Throws()
    {
        await using var db = CreateDb();
        SeedPaidDeposit(db, paidAmount: 100000, appliedAmount: 60000);
        await db.SaveChangesAsync();

        var service = new BookingDepositRefundService(db);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateRefundRequestAsync(new CreateBookingDepositRefundRequest
        {
            BookingDepositId = 1,
            Amount = 50000,
            Reason = BookingDepositRefundReasons.Other,
            IdempotencyKey = "manual:1"
        }, CancellationToken.None));
    }

    [Fact]
    public async Task CreateRefundRequestAsync_WhenCalledTwiceWithSameKey_ReturnsExistingRecord()
    {
        await using var db = CreateDb();
        SeedPaidDeposit(db, paidAmount: 100000);
        await db.SaveChangesAsync();
        var service = new BookingDepositRefundService(db);

        var first = await service.CreateRefundRequestAsync(NewRequest(60000, "same-key"), CancellationToken.None);
        var second = await service.CreateRefundRequestAsync(NewRequest(60000, "same-key"), CancellationToken.None);

        Assert.Equal(first.BookingDepositRefundId, second.BookingDepositRefundId);
        Assert.Equal(1, await db.BookingDepositRefunds.CountAsync());
    }

    [Fact]
    public async Task CalculateRefundableBalanceAsync_PendingRefundHoldsBalance()
    {
        await using var db = CreateDb();
        SeedPaidDeposit(db, paidAmount: 100000);
        db.BookingDepositRefunds.Add(new BookingDepositRefund
        {
            BookingDepositRefundId = 1,
            BookingDepositId = 1,
            BookingId = 1,
            RefundCode = "RF1",
            Amount = 60000,
            Status = BookingDepositRefundStatuses.PendingApproval,
            Reason = BookingDepositRefundReasons.Other,
            IdempotencyKey = "pending"
        });
        await db.SaveChangesAsync();

        var balance = await new BookingDepositRefundService(db).CalculateRefundableBalanceAsync(1, CancellationToken.None);

        Assert.Equal(40000, balance);
    }

    [Fact]
    public async Task CalculateRefundableBalanceAsync_RejectedAndFailedRefundsReleaseBalance()
    {
        await using var db = CreateDb();
        SeedPaidDeposit(db, paidAmount: 100000);
        db.BookingDepositRefunds.AddRange(
            ExistingRefund(1, BookingDepositRefundStatuses.Rejected, 60000, "rejected"),
            ExistingRefund(2, BookingDepositRefundStatuses.Failed, 30000, "failed"));
        await db.SaveChangesAsync();

        var balance = await new BookingDepositRefundService(db).CalculateRefundableBalanceAsync(1, CancellationToken.None);

        Assert.Equal(100000, balance);
    }

    [Fact]
    public async Task CompleteAsync_FromProcessing_IncreasesRefundedAmountOnce()
    {
        await using var db = CreateDb();
        SeedPaidDeposit(db, paidAmount: 100000);
        db.BookingDepositRefunds.Add(ExistingRefund(1, BookingDepositRefundStatuses.Processing, 60000, "processing"));
        await db.SaveChangesAsync();
        var service = new BookingDepositRefundService(db);

        await service.CompleteAsync(1, 99, "TRX1", CancellationToken.None);
        await service.CompleteAsync(1, 99, "TRX1", CancellationToken.None);

        var deposit = await db.BookingDeposits.SingleAsync();
        var refund = await db.BookingDepositRefunds.SingleAsync();
        Assert.Equal(60000, deposit.RefundedAmount);
        Assert.Equal(BookingDepositRefundStatuses.Succeeded, refund.Status);
    }

    [Fact]
    public async Task CompleteAsync_WhenPendingCustomerInfo_Throws()
    {
        await using var db = CreateDb();
        SeedPaidDeposit(db, paidAmount: 100000);
        db.BookingDepositRefunds.Add(ExistingRefund(1, BookingDepositRefundStatuses.PendingCustomerInfo, 60000, "pending"));
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            new BookingDepositRefundService(db).CompleteAsync(1, 99, "TRX1", CancellationToken.None));
    }

    [Fact]
    public async Task ApproveAsync_FromPendingApproval_Succeeds()
    {
        await using var db = CreateDb();
        SeedPaidDeposit(db, paidAmount: 100000);
        db.BookingDepositRefunds.Add(ExistingRefund(1, BookingDepositRefundStatuses.PendingApproval, 60000, "pending-approval", BookingDepositRefundMethods.CashAtVenue));
        await db.SaveChangesAsync();

        var result = await new BookingDepositRefundService(db).ApproveAsync(1, 99, CancellationToken.None);

        Assert.Equal(BookingDepositRefundStatuses.Approved, result.Status);
    }

    [Fact]
    public async Task PublicFlow_BankTransfer_EncryptsAccountAndMovesToPendingApproval()
    {
        await using var db = CreateDb();
        SeedPaidDeposit(db, paidAmount: 100000);
        await db.SaveChangesAsync();
        var email = new CapturingEmailService();
        var service = new BookingDepositRefundService(db, emailService: email, sensitiveDataProtector: new TestProtector());

        await service.CreateRefundRequestAsync(NewRequest(60000, "public-bank"), CancellationToken.None);
        var token = ExtractToken(email.ActionUrl);

        await service.SendVerificationCodeAsync(token, CancellationToken.None);
        await service.VerifyCustomerAsync(token, new VerifyDepositRefundRequest { VerificationCode = email.LastCode!, PhoneLast4 = "0000" }, CancellationToken.None);
        var result = await service.SubmitMethodAsync(token, new SubmitDepositRefundMethodRequest
        {
            RefundMethod = "BankTransfer",
            BankCode = "VCB",
            BankName = "Vietcombank",
            AccountNumber = "0123456789",
            ConfirmAccountNumber = "0123456789",
            AccountHolderName = "Vu Xuan Truong"
        }, CancellationToken.None);

        var refund = await db.BookingDepositRefunds.SingleAsync();
        Assert.Equal(BookingDepositRefundStatuses.PendingApproval, result.Status);
        Assert.Equal("6789", refund.CustomerBankAccountLast4);
        Assert.NotEqual("0123456789", refund.CustomerBankAccountNumberEncrypted);
        Assert.StartsWith("protected:", refund.CustomerBankAccountNumberEncrypted);
    }

    [Fact]
    public async Task CashPickup_Complete_IncreasesRefundedAmountOnce()
    {
        await using var db = CreateDb();
        SeedPaidDeposit(db, paidAmount: 100000);
        db.BookingDepositRefunds.Add(ExistingRefund(1, BookingDepositRefundStatuses.Approved, 60000, "cash", BookingDepositRefundMethods.CashAtVenue));
        await db.SaveChangesAsync();
        var email = new CapturingEmailService();
        var service = new BookingDepositRefundService(db, emailService: email);

        await service.PrepareCashPickupAsync(1, 99, CancellationToken.None);
        await service.CompleteCashPickupAsync(1, 99, new CompleteCashPickupRefundRequest
        {
            CashPickupCode = email.LastCode!,
            BookingCode = "BK1",
            PhoneLast4 = "0000"
        }, CancellationToken.None);

        var deposit = await db.BookingDeposits.SingleAsync();
        var refund = await db.BookingDepositRefunds.SingleAsync();
        Assert.Equal(60000, deposit.RefundedAmount);
        Assert.Equal(BookingDepositRefundStatuses.Succeeded, refund.Status);
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CompleteCashPickupAsync(1, 99, new CompleteCashPickupRefundRequest
        {
            CashPickupCode = email.LastCode!,
            BookingCode = "BK1",
            PhoneLast4 = "0000"
        }, CancellationToken.None));
    }

    [Fact]
    public async Task ApplyDepositToInvoice_WhenDepositIsLessThanInvoice_PartiallyPaysWithoutRefundOrForfeit()
    {
        await using var db = CreateDb();
        SeedPaidDeposit(db, paidAmount: 50000);
        var (session, invoice) = SeedSessionAndInvoice(db, grandTotal: 100000);
        await db.SaveChangesAsync();

        var result = await new BookingDepositRefundService(db).ApplyDepositToInvoiceAsync(session, invoice, CancellationToken.None);

        var deposit = await db.BookingDeposits.SingleAsync();
        Assert.Equal(50000, result.AppliedAmount);
        Assert.Equal(0, result.ExcessAmount);
        Assert.Equal(50000, deposit.AppliedAmount);
        Assert.Equal(0, deposit.ForfeitedAmount);
        Assert.Equal(0, deposit.RefundedAmount);
        Assert.Equal(InvoicePaymentStatuses.PartiallyPaid, invoice.PaymentStatus);
        Assert.Empty(await db.BookingDepositRefunds.ToListAsync());
    }

    [Fact]
    public async Task ApplyDepositToInvoice_WhenDepositEqualsInvoice_PaysWithoutRefundOrForfeit()
    {
        await using var db = CreateDb();
        SeedPaidDeposit(db, paidAmount: 50000);
        var (session, invoice) = SeedSessionAndInvoice(db, grandTotal: 50000);
        await db.SaveChangesAsync();

        var result = await new BookingDepositRefundService(db).ApplyDepositToInvoiceAsync(session, invoice, CancellationToken.None);

        var deposit = await db.BookingDeposits.SingleAsync();
        Assert.Equal(50000, result.AppliedAmount);
        Assert.Equal(0, result.ExcessAmount);
        Assert.Equal(50000, deposit.AppliedAmount);
        Assert.Equal(0, deposit.ForfeitedAmount);
        Assert.Equal(0, deposit.RefundedAmount);
        Assert.Equal(InvoicePaymentStatuses.Paid, invoice.PaymentStatus);
        Assert.Empty(await db.BookingDepositRefunds.ToListAsync());
    }

    [Fact]
    public async Task ApplyDepositToInvoice_WhenDepositExceedsInvoice_ForfeitsExcessWithoutRefund()
    {
        await using var db = CreateDb();
        SeedPaidDeposit(db, paidAmount: 50000);
        var (session, invoice) = SeedSessionAndInvoice(db, grandTotal: 2667);
        await db.SaveChangesAsync();

        var service = new BookingDepositRefundService(db);
        var result = await service.ApplyDepositToInvoiceAsync(session, invoice, CancellationToken.None);
        await service.ApplyDepositToInvoiceAsync(session, invoice, CancellationToken.None);

        var deposit = await db.BookingDeposits.SingleAsync();
        var payment = await db.Payments.SingleAsync();
        var summary = await service.GetSummaryForInvoiceAsync(invoice.InvoiceId, CancellationToken.None);

        Assert.Equal(2667, result.AppliedAmount);
        Assert.Equal(47333, result.ExcessAmount);
        Assert.Null(result.ExcessRefund);
        Assert.Equal(2667, deposit.AppliedAmount);
        Assert.Equal(47333, deposit.ForfeitedAmount);
        Assert.Equal(0, deposit.RefundedAmount);
        Assert.Equal(2667, payment.Amount);
        Assert.Equal(2667, invoice.PaidAmount);
        Assert.Equal(InvoicePaymentStatuses.Paid, invoice.PaymentStatus);
        Assert.Empty(await db.BookingDepositRefunds.ToListAsync());
        Assert.NotNull(summary);
        Assert.Equal(50000, summary!.PaidAmount);
        Assert.Equal(2667, summary.AppliedAmount);
        Assert.Equal(47333, summary.ForfeitedAmount);
        Assert.Equal(0, summary.PendingRefundAmount);
        Assert.Equal(0, summary.RefundedAmount);
        Assert.Equal(0, summary.RefundableBalance);
        Assert.Empty(summary.RefundRequests);
    }

    private static CreateBookingDepositRefundRequest NewRequest(decimal amount, string key) => new()
    {
        BookingDepositId = 1,
        Amount = amount,
        Reason = BookingDepositRefundReasons.Other,
        IdempotencyKey = key
    };

    private static BookingDepositRefund ExistingRefund(long id, int status, decimal amount, string key, int? method = null) => new()
    {
        BookingDepositRefundId = id,
        BookingDepositId = 1,
        BookingId = 1,
        RefundCode = $"RF{id}",
        Amount = amount,
        Status = status,
        RefundMethod = method,
        Reason = BookingDepositRefundReasons.Other,
        IdempotencyKey = key,
        CustomerEmailSnapshot = "customer@example.com",
        CustomerPhoneSnapshot = "0900000000"
    };

    private static void SeedPaidDeposit(PoolHubDbContext db, decimal paidAmount, decimal appliedAmount = 0)
    {
        db.Customers.Add(new Customer { CustomerId = 1, FullName = "Customer", PhoneNumber = "0900000000", Email = "customer@example.com", Status = true });
        db.Bookings.Add(new Booking { BookingId = 1, BookingCode = "BK1", CustomerId = 1, StartTimeUtc = DateTime.UtcNow.AddHours(3), EndTimeUtc = DateTime.UtcNow.AddHours(4), NumberOfGuests = 2, Status = BookingStatuses.Confirmed });
        db.BookingDeposits.Add(new BookingDeposit
        {
            BookingDepositId = 1,
            BookingId = 1,
            RequiredAmount = paidAmount,
            PaidAmount = paidAmount,
            AppliedAmount = appliedAmount,
            Status = BookingDepositStatuses.Paid,
            PaidAtUtc = DateTime.UtcNow.AddMinutes(-5),
            DueAtUtc = DateTime.UtcNow.AddMinutes(5)
        });
    }

    private static (Session session, Invoice invoice) SeedSessionAndInvoice(PoolHubDbContext db, decimal grandTotal)
    {
        var session = new Session
        {
            SessionId = 1,
            SessionCode = "S1",
            BookingId = 1,
            CustomerId = 1,
            OpenedByUserId = 99,
            StartedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            Status = 1
        };
        var invoice = new Invoice
        {
            InvoiceId = 1,
            InvoiceCode = "INV1",
            SessionId = session.SessionId,
            CustomerId = 1,
            GrandTotalAmount = grandTotal,
            PaymentStatus = InvoicePaymentStatuses.Unpaid,
            Status = 1
        };
        db.Sessions.Add(session);
        db.Invoices.Add(invoice);
        return (session, invoice);
    }

    private static PoolHubDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PoolHubDbContext(options);
    }

    private static string ExtractToken(string? url)
    {
        Assert.False(string.IsNullOrWhiteSpace(url));
        return url!.Split('/').Last();
    }

    private sealed class TestProtector : ISensitiveDataProtector
    {
        public string Protect(string plainText) => $"protected:{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plainText))}";
        public string Unprotect(string protectedText) => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(protectedText["protected:".Length..]));
    }

    private sealed class CapturingEmailService : IEmailService
    {
        public string? ActionUrl { get; private set; }
        public string? LastCode { get; private set; }
        public void EnsureConfigured() { }
        public Task SendPasswordResetOtpAsync(string email, string otp, int expirationMinutes, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SendBookingConfirmedAsync(string email, string customerName, string phoneNumber, string bookingCode, string tableName, DateTime startTimeUtc, DateTime endTimeUtc, int numberOfGuests, CancellationToken ct) => Task.CompletedTask;
        public Task SendBookingCancelledAsync(string email, string customerName, string phoneNumber, string bookingCode, string tableName, DateTime startTimeUtc, DateTime endTimeUtc, int numberOfGuests, string reason, CancellationToken ct) => Task.CompletedTask;
        public Task SendDepositRefundNotificationAsync(string email, string subject, string title, string message, IReadOnlyDictionary<string, string> details, string? actionUrl, string? actionText, CancellationToken ct)
        {
            ActionUrl = actionUrl ?? ActionUrl;
            var match = System.Text.RegularExpressions.Regex.Match(message, "\\b\\d{6}\\b");
            if (match.Success) LastCode = match.Value;
            return Task.CompletedTask;
        }
    }
}
