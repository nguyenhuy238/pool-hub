using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.BookingDepositRefund;
using PoolHub.Core.Entities;
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
        db.BookingDepositRefunds.Add(ExistingRefund(1, BookingDepositRefundStatuses.PendingApproval, 60000, "pending-approval"));
        await db.SaveChangesAsync();

        var result = await new BookingDepositRefundService(db).ApproveAsync(1, 99, CancellationToken.None);

        Assert.Equal(BookingDepositRefundStatuses.Approved, result.Status);
    }

    private static CreateBookingDepositRefundRequest NewRequest(decimal amount, string key) => new()
    {
        BookingDepositId = 1,
        Amount = amount,
        Reason = BookingDepositRefundReasons.Other,
        IdempotencyKey = key
    };

    private static BookingDepositRefund ExistingRefund(long id, int status, decimal amount, string key) => new()
    {
        BookingDepositRefundId = id,
        BookingDepositId = 1,
        BookingId = 1,
        RefundCode = $"RF{id}",
        Amount = amount,
        Status = status,
        Reason = BookingDepositRefundReasons.Other,
        IdempotencyKey = key
    };

    private static void SeedPaidDeposit(PoolHubDbContext db, decimal paidAmount, decimal appliedAmount = 0)
    {
        db.Customers.Add(new Customer { CustomerId = 1, FullName = "Customer", PhoneNumber = "0900000000", Status = true });
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

    private static PoolHubDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PoolHubDbContext(options);
    }
}
