using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.BookingDepositRefund;
using PoolHub.Core.Entities;
using PoolHub.Infrastructure.Data;
using PoolHub.Services.Booking;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Exceptions;
using Xunit;

namespace PoolHub.IntegrationTests.Booking;

public class BookingDepositRefundConcurrencyTests
{
    [Fact]
    public async Task CompleteSameRefundConcurrently_IncrementsRefundedAmountOnce()
    {
        await using var database = await SqlServerRefundTestDatabase.CreateAsync();
        long refundId;
        long actorUserId;
        await using (var db = database.CreateContext())
        {
            actorUserId = await SeedUserAsync(db);
            var seededDeposit = await SeedDepositAsync(db, paidAmount: 200000);
            var seededRefund = NewRefund(seededDeposit, 100000, BookingDepositRefundStatuses.Processing, "complete-once");
            db.BookingDepositRefunds.Add(seededRefund);
            await db.SaveChangesAsync();
            refundId = seededRefund.BookingDepositRefundId;
        }

        async Task CompleteAsync()
        {
            await using var db = database.CreateContext();
            await new BookingDepositRefundService(db).CompleteAsync(refundId, actorUserId, "FT-CONCURRENT", CancellationToken.None);
        }

        var results = await Task.WhenAll(CaptureAsync(CompleteAsync), CaptureAsync(CompleteAsync));

        await using var verify = database.CreateContext();
        var completedDeposit = await verify.BookingDeposits.SingleAsync();
        var completedRefund = await verify.BookingDepositRefunds.SingleAsync();

        Assert.True(results.Any(x => x is null), string.Join(" | ", results.Where(x => x is not null).Select(x => Describe(x!))));
        Assert.Equal(BookingDepositRefundStatuses.Succeeded, completedRefund.Status);
        Assert.Equal(100000, completedDeposit.RefundedAmount);
    }

    [Fact]
    public async Task TwoRefundsCannotConsumeMoreThanAvailableBalance()
    {
        await using var database = await SqlServerRefundTestDatabase.CreateAsync();
        long refundAId;
        long refundBId;
        long actorUserId;
        await using (var db = database.CreateContext())
        {
            actorUserId = await SeedUserAsync(db);
            var seededDeposit = await SeedDepositAsync(db, paidAmount: 200000, appliedAmount: 100000);
            var refundA = NewRefund(seededDeposit, 80000, BookingDepositRefundStatuses.Processing, "refund-a");
            var refundB = NewRefund(seededDeposit, 80000, BookingDepositRefundStatuses.Processing, "refund-b");
            db.BookingDepositRefunds.Add(refundA);
            db.BookingDepositRefunds.Add(refundB);
            await db.SaveChangesAsync();
            refundAId = refundA.BookingDepositRefundId;
            refundBId = refundB.BookingDepositRefundId;
        }

        async Task CompleteAsync(long id)
        {
            await using var db = database.CreateContext();
            await new BookingDepositRefundService(db).CompleteAsync(id, actorUserId, $"FT-{id}", CancellationToken.None);
        }

        await Task.WhenAll(CaptureAsync(() => CompleteAsync(refundAId)), CaptureAsync(() => CompleteAsync(refundBId)));

        await using var verify = database.CreateContext();
        var succeeded = await verify.BookingDepositRefunds
            .Where(x => x.Status == BookingDepositRefundStatuses.Succeeded)
            .SumAsync(x => x.Amount);
        var checkedDeposit = await verify.BookingDeposits.SingleAsync();

        Assert.True(succeeded <= 100000);
        Assert.True(checkedDeposit.RefundedAmount <= 100000);
    }

    [Fact]
    public async Task DuplicateIdempotencyKeyCreatesSingleRefund()
    {
        await using var database = await SqlServerRefundTestDatabase.CreateAsync();
        long depositId;
        await using (var db = database.CreateContext())
        {
            var seededDeposit = await SeedDepositAsync(db, paidAmount: 200000);
            depositId = seededDeposit.BookingDepositId;
        }

        async Task CreateAsync()
        {
            await using var db = database.CreateContext();
            await new BookingDepositRefundService(db).CreateRefundRequestAsync(new CreateBookingDepositRefundRequest
            {
                BookingDepositId = depositId,
                Amount = 50000,
                Reason = BookingDepositRefundReasons.DepositExcess,
                IdempotencyKey = "deposit-excess:1:1"
            }, CancellationToken.None);
        }

        var results = await Task.WhenAll(CaptureAsync(CreateAsync), CaptureAsync(CreateAsync));

        await using var verify = database.CreateContext();
        var count = await verify.BookingDepositRefunds.CountAsync(x => x.IdempotencyKey == "deposit-excess:1:1");
        Assert.True(count == 1, $"count={count}; " + string.Join(" | ", results.Where(x => x is not null).Select(x => Describe(x!))));
        Assert.Contains(results, x => x is null);
    }

    private static async Task<Exception?> CaptureAsync(Func<Task> action)
    {
        try
        {
            await action();
            return null;
        }
        catch (Exception ex) when (ex is BusinessRuleException or DbUpdateConcurrencyException or DbUpdateException)
        {
            return ex;
        }
    }

    private static string Describe(Exception ex) =>
        ex.GetType().Name + ": " + ex.Message + (ex.InnerException is null ? string.Empty : " => " + ex.InnerException.Message);

    private static async Task<long> SeedUserAsync(PoolHubDbContext db)
    {
        var user = new User
        {
            FullName = "Cashier",
            Email = $"cashier-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            EmailConfirmed = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.UserId;
    }

    private static async Task<BookingDeposit> SeedDepositAsync(PoolHubDbContext db, decimal paidAmount, decimal appliedAmount = 0)
    {
        var customer = new Customer { FullName = "Customer", PhoneNumber = "0900000000", Email = "customer@example.com", Status = true };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var booking = new PoolHub.Core.Entities.Booking { BookingCode = "BKSQL1", CustomerId = customer.CustomerId, StartTimeUtc = DateTime.UtcNow.AddHours(1), EndTimeUtc = DateTime.UtcNow.AddHours(2), NumberOfGuests = 2, Status = BookingStatuses.Confirmed };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var deposit = new BookingDeposit
        {
            BookingId = booking.BookingId,
            RequiredAmount = paidAmount,
            PaidAmount = paidAmount,
            AppliedAmount = appliedAmount,
            Status = BookingDepositStatuses.Paid,
            PaidAtUtc = DateTime.UtcNow.AddMinutes(-5),
            DueAtUtc = DateTime.UtcNow.AddMinutes(5)
        };
        db.BookingDeposits.Add(deposit);
        await db.SaveChangesAsync();
        return deposit;
    }

    private static BookingDepositRefund NewRefund(BookingDeposit deposit, decimal amount, int status, string key) => new()
    {
        BookingDepositId = deposit.BookingDepositId,
        BookingId = deposit.BookingId,
        RefundCode = "RF" + Guid.NewGuid().ToString("N")[..10],
        Amount = amount,
        Status = status,
        RefundMethod = BookingDepositRefundMethods.BankTransfer,
        Reason = BookingDepositRefundReasons.DepositExcess,
        IdempotencyKey = key,
        CustomerEmailSnapshot = "customer@example.com",
        CustomerPhoneSnapshot = "0900000000"
    };

    private sealed class SqlServerRefundTestDatabase : IAsyncDisposable
    {
        private readonly string _connectionString;

        private SqlServerRefundTestDatabase(string connectionString)
        {
            _connectionString = connectionString;
        }

        public static async Task<SqlServerRefundTestDatabase> CreateAsync()
        {
            var configured = Environment.GetEnvironmentVariable("POOLHUB_TEST_SQLSERVER");
            var dbName = "PoolHubRefundConcurrency_" + Guid.NewGuid().ToString("N");
            var connectionString = string.IsNullOrWhiteSpace(configured)
                ? $"Server=localhost;Database={dbName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
                : configured.Replace("{Database}", dbName, StringComparison.OrdinalIgnoreCase);

            var database = new SqlServerRefundTestDatabase(connectionString);
            await using var db = database.CreateContext();
            await db.Database.EnsureCreatedAsync();
            return database;
        }

        public PoolHubDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<PoolHubDbContext>()
                .UseSqlServer(_connectionString)
                .Options;
            return new PoolHubDbContext(options);
        }

        public async ValueTask DisposeAsync()
        {
            await using var db = CreateContext();
            await db.Database.EnsureDeletedAsync();
        }
    }
}
