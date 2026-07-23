using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Booking;
using PoolHub.Core.Entities;
using PoolHub.Infrastructure.Data;
using PoolHub.Services.Booking;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Exceptions;
using Xunit;

namespace PoolHub.UnitTests;

public class BookingServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenExistingCustomerIsBlocked_ThrowsBusinessRuleException()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new PoolHubDbContext(options);
        db.Customers.Add(new Customer { CustomerId = 1, FullName = "Blocked Customer", PhoneNumber = "0900000000", Status = false });
        await db.SaveChangesAsync();

        var service = new BookingService(db, new TestEmailService(), new Microsoft.Extensions.Logging.Abstractions.NullLogger<BookingService>());
        var request = new CreateBookingRequest
        {
            CustomerId = 1,
            StartTimeUtc = new DateTime(2026, 6, 24, 10, 0, 0, DateTimeKind.Utc),
            EndTimeUtc = new DateTime(2026, 6, 24, 11, 0, 0, DateTimeKind.Utc),
            NumberOfGuests = 2
        };

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CreateAsync(request, CancellationToken.None));

        Assert.Equal("Customer is blocked or inactive.", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenPhoneBelongsToBlockedCustomer_ThrowsBusinessRuleException()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new PoolHubDbContext(options);
        db.Customers.Add(new Customer { CustomerId = 1, FullName = "Blocked Customer", PhoneNumber = "0900000000", Status = false });
        await db.SaveChangesAsync();

        var service = new BookingService(db, new TestEmailService(), new Microsoft.Extensions.Logging.Abstractions.NullLogger<BookingService>());
        var request = new CreateBookingRequest
        {
            PhoneNumber = "0900000000",
            CustomerName = "Blocked Customer",
            StartTimeUtc = new DateTime(2026, 6, 24, 10, 0, 0, DateTimeKind.Utc),
            EndTimeUtc = new DateTime(2026, 6, 24, 11, 0, 0, DateTimeKind.Utc),
            NumberOfGuests = 2
        };

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CreateAsync(request, CancellationToken.None));

        Assert.Equal("Customer is blocked or inactive.", exception.Message);
    }

    [Fact]
    public async Task GetBookingsAsync_AutomaticallyTransitionsExpiredBookings()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new PoolHubDbContext(options);
        var now = DateTime.UtcNow;
        db.Customers.Add(new Customer { CustomerId = 1, FullName = "Customer", PhoneNumber = "0900000001", Status = true });
        db.Bookings.AddRange(
            NewBooking(1, BookingStatuses.Pending, now.AddHours(-1), now.AddHours(1)),
            NewBooking(2, BookingStatuses.Confirmed, now.AddHours(-2), now.AddHours(-1)),
            NewBooking(3, BookingStatuses.Confirmed, now.AddMinutes(-30), now.AddMinutes(30)));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.GetBookingsAsync(new BookingQueryRequest { PageNumber = 1, PageSize = 20 }, CancellationToken.None);

        Assert.Equal(BookingStatuses.Cancelled, (await db.Bookings.FindAsync(1L))!.Status);
        Assert.NotNull((await db.Bookings.FindAsync(1L))!.CancelledAtUtc);
        Assert.Equal(BookingStatuses.NoShow, (await db.Bookings.FindAsync(2L))!.Status);
        Assert.Equal(BookingStatuses.Confirmed, (await db.Bookings.FindAsync(3L))!.Status);
    }

    [Fact]
    public async Task GetBookingsAsync_WhenBookingHasSession_DoesNotAutomaticallyTransition()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new PoolHubDbContext(options);
        var booking = NewBooking(1, BookingStatuses.Confirmed, DateTime.UtcNow.AddHours(-2), DateTime.UtcNow.AddHours(-1));
        db.Bookings.Add(booking);
        db.Sessions.Add(new Session { SessionId = 1, SessionCode = "SS1", BookingId = 1, Status = 2, StartedAtUtc = booking.StartTimeUtc, OpenedByUserId = 1 });
        await db.SaveChangesAsync();

        await CreateService(db).GetBookingsAsync(new BookingQueryRequest { PageNumber = 1, PageSize = 20 }, CancellationToken.None);

        Assert.Equal(BookingStatuses.Confirmed, booking.Status);
    }

    [Fact]
    public async Task ConfirmAsync_WhenPendingBookingReachedStart_CancelsAndThrowsConflict()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new PoolHubDbContext(options);
        var booking = NewBooking(1, BookingStatuses.Pending, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddHours(1));
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() => CreateService(db).ConfirmAsync(1, 1, CancellationToken.None));

        Assert.Equal(BookingStatuses.Cancelled, booking.Status);
        Assert.NotNull(booking.CancelledAtUtc);
    }

    [Fact]
    public async Task Availability_UsesEndExclusiveOverlapAcrossMidnight()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new PoolHubDbContext(options);
        db.TableTypes.Add(new TableType { TableTypeId = 1, Name = "Standard" });
        db.VenueTables.Add(new VenueTable { TableId = 1, TableTypeId = 1, ZoneId = 1, TableCode = "T01", TableName = "Table 01", Capacity = 4, IsActive = true, OperationalStatus = 1 });
        var startUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(2).AddHours(16), DateTimeKind.Utc);
        var endUtc = startUtc.AddHours(3);
        var overnightBooking = NewBooking(
            1,
            BookingStatuses.Confirmed,
            startUtc,
            endUtc);
        overnightBooking.TableId = 1;
        db.Bookings.Add(overnightBooking);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var overlapping = await service.GetAvailabilityAsync(new BookingAvailabilityRequest
        {
            TableId = 1,
            StartTimeUtc = endUtc.AddHours(-1),
            EndTimeUtc = endUtc.AddHours(1)
        }, CancellationToken.None);
        var touchingEnd = await service.GetAvailabilityAsync(new BookingAvailabilityRequest
        {
            TableId = 1,
            StartTimeUtc = endUtc,
            EndTimeUtc = endUtc.AddHours(2)
        }, CancellationToken.None);

        Assert.Empty(overlapping);
        Assert.Contains(touchingEnd, table => table.TableId == 1);
    }

    [Fact]
    public async Task GetBookingsAsync_DateFilter_UsesVietnamLocalDayRange()
    {
        await using var db = CreateDb();
        db.Customers.Add(new Customer { CustomerId = 1, FullName = "Customer", PhoneNumber = "0900000001", Status = true });
        db.Bookings.AddRange(
            NewBooking(1, BookingStatuses.Confirmed, new DateTime(2026, 7, 9, 16, 59, 0, DateTimeKind.Utc), new DateTime(2026, 7, 9, 17, 30, 0, DateTimeKind.Utc)),
            NewBooking(2, BookingStatuses.Confirmed, new DateTime(2026, 7, 9, 17, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 9, 18, 0, 0, DateTimeKind.Utc)),
            NewBooking(3, BookingStatuses.Confirmed, new DateTime(2026, 7, 10, 16, 59, 0, DateTimeKind.Utc), new DateTime(2026, 7, 10, 18, 0, 0, DateTimeKind.Utc)));
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetBookingsAsync(new BookingQueryRequest
        {
            Date = new DateTime(2026, 7, 10),
            PageNumber = 1,
            PageSize = 20
        }, CancellationToken.None);

        Assert.DoesNotContain(result.Items, x => x.BookingId == 1);
        Assert.Contains(result.Items, x => x.BookingId == 2);
        Assert.Contains(result.Items, x => x.BookingId == 3);
    }

    [Theory]
    [InlineData(100000, 50000)]
    [InlineData(180000, 54000)]
    [InlineData(181001, 55000)]
    public void CalculateDepositRequiredAmount_UsesPercentMinimumAndRoundUp(decimal estimated, decimal expected)
    {
        Assert.Equal(expected, BookingService.CalculateDepositRequiredAmount(estimated));
    }

    [Fact]
    public async Task CreatePublicAsync_ForSingleTable_CreatesPendingDeposit()
    {
        await using var db = CreateDb();
        await SeedBookingBasicsAsync(db, tableCount: 2);
        var service = CreateService(db);

        var booking = await service.CreatePublicAsync(NewCreateRequest(tableId: 1), CancellationToken.None);

        Assert.Equal(BookingStatuses.PendingDeposit, booking.Status);
        Assert.NotNull(booking.HoldExpiresAtUtc);
        Assert.NotNull(booking.Deposit);
        Assert.NotNull(booking.DepositPaymentInstruction);
        Assert.Equal(50000, booking.Deposit!.RequiredAmount);
        Assert.Contains("POOLHUB", booking.DepositPaymentInstruction!.TransferContent);
        Assert.Contains("amount=50000", booking.DepositPaymentInstruction.VietQrUrl);
    }

    [Fact]
    public async Task MockPayDepositAsync_WhenPendingDeposit_ConfirmsBooking()
    {
        await using var db = CreateDb();
        await SeedBookingBasicsAsync(db, tableCount: 2);
        var service = CreateService(db);
        var booking = await service.CreatePublicAsync(NewCreateRequest(tableId: 1), CancellationToken.None);

        var paid = await service.MockPayDepositAsync(booking.BookingId, CancellationToken.None);

        Assert.Equal(BookingStatuses.Confirmed, paid.Status);
        Assert.Equal(BookingDepositStatuses.Paid, paid.Deposit!.Status);
        Assert.Equal(paid.Deposit.RequiredAmount, paid.Deposit.PaidAmount);
    }

    [Fact]
    public async Task CancelAsync_WhenCustomerCancelsAtLeast120MinutesBeforeStart_CreatesPendingRefund()
    {
        await using var db = CreateDb();
        await SeedBookingBasicsAsync(db, tableCount: 1);
        var start = DateTime.UtcNow.AddMinutes(130);
        db.Bookings.Add(NewBooking(1, BookingStatuses.Confirmed, start, start.AddHours(1)));
        db.BookingDeposits.Add(new BookingDeposit { BookingDepositId = 1, BookingId = 1, RequiredAmount = 50000, PaidAmount = 50000, Status = BookingDepositStatuses.Paid, PaidAtUtc = DateTime.UtcNow, DueAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        await CreateService(db).CancelAsync(1, new CancelBookingRequest { Reason = "Customer cancelled" }, CancellationToken.None);

        var deposit = await db.BookingDeposits.SingleAsync();
        var refund = await db.BookingDepositRefunds.SingleAsync();
        Assert.Equal(BookingStatuses.Cancelled, (await db.Bookings.FindAsync(1L))!.Status);
        Assert.Equal(0, deposit.RefundedAmount);
        Assert.Equal(50000, refund.Amount);
        Assert.Equal(BookingDepositRefundReasons.CustomerCancelledInTime, refund.Reason);
        Assert.Equal(BookingDepositRefundStatuses.PendingCustomerInfo, refund.Status);
    }

    [Fact]
    public async Task CancelAsync_WhenCustomerCancelsExactly120MinutesBeforeStart_CreatesPendingRefund()
    {
        await using var db = CreateDb();
        await SeedBookingBasicsAsync(db, tableCount: 1);
        var start = DateTime.UtcNow.AddMinutes(120).AddSeconds(1);
        db.Bookings.Add(NewBooking(1, BookingStatuses.Confirmed, start, start.AddHours(1)));
        db.BookingDeposits.Add(new BookingDeposit { BookingDepositId = 1, BookingId = 1, RequiredAmount = 50000, PaidAmount = 50000, Status = BookingDepositStatuses.Paid, PaidAtUtc = DateTime.UtcNow, DueAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        await CreateService(db).CancelAsync(1, new CancelBookingRequest { Reason = "Customer cancelled" }, CancellationToken.None);

        Assert.Equal(1, await db.BookingDepositRefunds.CountAsync());
    }

    [Fact]
    public async Task CancelAsync_WhenCustomerCancelsUnder120Minutes_ForfeitsDeposit()
    {
        await using var db = CreateDb();
        await SeedBookingBasicsAsync(db, tableCount: 1);
        var start = DateTime.UtcNow.AddMinutes(90);
        db.Bookings.Add(NewBooking(1, BookingStatuses.Confirmed, start, start.AddHours(1)));
        db.BookingDeposits.Add(new BookingDeposit { BookingDepositId = 1, BookingId = 1, RequiredAmount = 50000, PaidAmount = 50000, Status = BookingDepositStatuses.Paid, PaidAtUtc = DateTime.UtcNow, DueAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        await CreateService(db).CancelAsync(1, new CancelBookingRequest { Reason = "Customer cancelled" }, CancellationToken.None);

        var deposit = await db.BookingDeposits.SingleAsync();
        Assert.Equal(BookingDepositStatuses.Forfeited, deposit.Status);
        Assert.Equal(50000, deposit.ForfeitedAmount);
        Assert.Empty(await db.BookingDepositRefunds.ToListAsync());
    }

    [Fact]
    public async Task CancelAsync_WhenCancelledByVenue_CreatesVenueFaultRefund()
    {
        await using var db = CreateDb();
        await SeedBookingBasicsAsync(db, tableCount: 1);
        var start = DateTime.UtcNow.AddMinutes(30);
        db.Bookings.Add(NewBooking(1, BookingStatuses.Confirmed, start, start.AddHours(1)));
        db.BookingDeposits.Add(new BookingDeposit { BookingDepositId = 1, BookingId = 1, RequiredAmount = 50000, PaidAmount = 50000, Status = BookingDepositStatuses.Paid, PaidAtUtc = DateTime.UtcNow, DueAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        await CreateService(db).CancelAsync(1, new CancelBookingRequest { Reason = "Venue fault", CancelledByVenue = true }, CancellationToken.None);

        var refund = await db.BookingDepositRefunds.SingleAsync();
        Assert.Equal(BookingDepositRefundReasons.VenueFault, refund.Reason);
        Assert.Equal(50000, refund.Amount);
    }

    [Fact]
    public async Task Availability_PendingDepositExpired_DoesNotBlockSlot()
    {
        await using var db = CreateDb();
        await SeedBookingBasicsAsync(db, tableCount: 1);
        var start = new DateTime(2026, 7, 4, 10, 0, 0, DateTimeKind.Utc);
        var booking = NewBooking(1, BookingStatuses.PendingDeposit, start, start.AddHours(1));
        booking.TableId = 1;
        booking.HoldExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
        db.Bookings.Add(booking);
        db.BookingDeposits.Add(new BookingDeposit { BookingId = 1, RequiredAmount = 50000, Status = BookingDepositStatuses.Pending, DueAtUtc = DateTime.UtcNow.AddMinutes(-1) });
        await db.SaveChangesAsync();

        var availability = await CreateService(db).GetAvailabilityAsync(new BookingAvailabilityRequest { TableId = 1, StartTimeUtc = start, EndTimeUtc = start.AddHours(1) }, CancellationToken.None);

        Assert.Contains(availability, x => x.TableId == 1);
        Assert.Equal(BookingStatuses.Expired, booking.Status);
    }

    [Fact]
    public async Task Availability_ConfirmedBooking_BlocksSlot()
    {
        await using var db = CreateDb();
        await SeedBookingBasicsAsync(db, tableCount: 1);
        var start = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(1).AddHours(10), DateTimeKind.Utc);
        var booking = NewBooking(1, BookingStatuses.Confirmed, start, start.AddHours(1));
        booking.TableId = 1;
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var availability = await CreateService(db).GetAvailabilityAsync(new BookingAvailabilityRequest { TableId = 1, StartTimeUtc = start.AddMinutes(30), EndTimeUtc = start.AddHours(2) }, CancellationToken.None);

        Assert.Empty(availability);
    }

    [Fact]
    public async Task CreatePublicAsync_ForLargeBooking_CreatesPendingDepositWithoutApproval()
    {
        await using var db = CreateDb();
        await SeedBookingBasicsAsync(db, tableCount: 5);
        var service = CreateService(db);
        var request = NewCreateRequest(tableId: 1);
        request.TableIds = [1, 2, 3, 4];

        var booking = await service.CreatePublicAsync(request, CancellationToken.None);

        Assert.Equal(BookingStatuses.PendingDeposit, booking.Status);
        Assert.False(booking.RequiresApproval);
        Assert.NotNull(booking.HoldExpiresAtUtc);
    }

    [Fact]
    public async Task CreateAsync_WhenAllAvailableTablesRequested_CreatesOneBookingWithAllTables()
    {
        await using var db = CreateDb();
        await SeedBookingBasicsAsync(db, tableCount: 4);
        var service = CreateService(db);
        var request = NewCreateRequest(tableId: 1);
        request.TableIds = [1, 2, 3, 4];

        var booking = await service.CreateAsync(request, CancellationToken.None);

        Assert.Equal([1, 2, 3, 4], booking.TableIds.OrderBy(x => x).ToList());
        Assert.Equal(4, await db.BookingTables.CountAsync(x => x.BookingId == booking.BookingId));
    }

    [Fact]
    public async Task CreateAsync_WhenAnyRequestedTableHasActiveSession_RejectsWholeBookingWithoutPartialWrites()
    {
        await using var db = CreateDb();
        await SeedBookingBasicsAsync(db, tableCount: 4);
        db.Sessions.Add(NewSession(1, status: 1));
        db.SessionTableAssignments.Add(NewAssignment(1, tableId: 3));
        await db.SaveChangesAsync();
        var request = NewCreateRequest(tableId: 1);
        request.TableIds = [1, 2, 3, 4];

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            CreateService(db).CreateAsync(request, CancellationToken.None));

        Assert.Contains("currently in use", exception.Message);
        Assert.Empty(db.Bookings);
        Assert.Empty(db.BookingTables);
    }

    [Fact]
    public async Task CreateAsync_DirectServiceCallWithoutAvailability_WhenTableHasActiveSession_Rejects()
    {
        await using var db = CreateDb();
        await SeedBookingBasicsAsync(db, tableCount: 2);
        db.Sessions.Add(NewSession(1, status: 1));
        db.SessionTableAssignments.Add(NewAssignment(1, tableId: 2));
        await db.SaveChangesAsync();
        var request = NewCreateRequest(tableId: 2);
        request.TableIds = [2];

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateService(db).CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_WhenAddingTableWithActiveSession_RejectsAndKeepsOldTables()
    {
        await using var db = CreateDb();
        await SeedBookingBasicsAsync(db, tableCount: 3);
        var existing = NewBooking(1, BookingStatuses.PendingDeposit, DateTime.UtcNow.Date.AddDays(1).AddHours(10), DateTime.UtcNow.Date.AddDays(1).AddHours(11));
        existing.TableId = 1;
        existing.TableTypeId = 1;
        db.Bookings.Add(existing);
        db.BookingTables.Add(new BookingTable { BookingId = 1, TableId = 1 });
        db.Sessions.Add(NewSession(1, status: 1));
        db.SessionTableAssignments.Add(NewAssignment(1, tableId: 2));
        await db.SaveChangesAsync();

        var update = NewUpdateRequest([1, 2]);

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateService(db).UpdateAsync(1, update, CancellationToken.None));

        Assert.Equal(1, existing.TableId);
        Assert.Equal([1], await db.BookingTables.Where(x => x.BookingId == 1).Select(x => x.TableId).OrderBy(x => x).ToListAsync());
    }

    [Fact]
    public async Task UpdateAsync_WhenTableIdsEmpty_RejectsAndKeepsOldTables()
    {
        await using var db = CreateDb();
        await SeedBookingBasicsAsync(db, tableCount: 2);
        var existing = NewBooking(1, BookingStatuses.PendingDeposit, DateTime.UtcNow.Date.AddDays(1).AddHours(10), DateTime.UtcNow.Date.AddDays(1).AddHours(11));
        existing.TableId = 1;
        existing.TableTypeId = 1;
        db.Bookings.Add(existing);
        db.BookingTables.Add(new BookingTable { BookingId = 1, TableId = 1 });
        await db.SaveChangesAsync();

        var update = NewUpdateRequest([]);

        await Assert.ThrowsAsync<ValidationException>(() =>
            CreateService(db).UpdateAsync(1, update, CancellationToken.None));

        Assert.Equal(1, existing.TableId);
        Assert.Equal([1], await db.BookingTables.Where(x => x.BookingId == 1).Select(x => x.TableId).ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_WhenRequestedTableIsMaintenance_Rejects()
    {
        await using var db = CreateDb();
        await SeedBookingBasicsAsync(db, tableCount: 3);
        (await db.VenueTables.FindAsync(3L))!.OperationalStatus = 4;
        await db.SaveChangesAsync();
        var request = NewCreateRequest(tableId: 1);
        request.TableIds = [1, 3];

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateService(db).CreateAsync(request, CancellationToken.None));

        Assert.Empty(db.Bookings);
        Assert.Empty(db.BookingTables);
    }

    [Fact]
    public async Task CreateAsync_WhenAnyRequestedTableOverlapsExistingBooking_Rejects()
    {
        await using var db = CreateDb();
        await SeedBookingBasicsAsync(db, tableCount: 3);
        var request = NewCreateRequest(tableId: 1);
        var existing = NewBooking(99, BookingStatuses.Confirmed, request.StartTimeUtc.AddMinutes(15), request.EndTimeUtc.AddMinutes(15));
        existing.TableId = 2;
        existing.TableTypeId = 1;
        db.Bookings.Add(existing);
        await db.SaveChangesAsync();
        request.TableIds = [1, 2];

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateService(db).CreateAsync(request, CancellationToken.None));

        Assert.Single(db.Bookings);
    }

    [Fact]
    public async Task CreateAsync_WhenSessionAssignmentEnded_AllowsBooking()
    {
        await using var db = CreateDb();
        await SeedBookingBasicsAsync(db, tableCount: 2);
        db.Sessions.Add(NewSession(1, status: 1));
        db.SessionTableAssignments.Add(NewAssignment(1, tableId: 2, endedAtUtc: DateTime.UtcNow.AddMinutes(-5)));
        await db.SaveChangesAsync();
        var request = NewCreateRequest(tableId: 1);
        request.TableIds = [1, 2];

        var booking = await CreateService(db).CreateAsync(request, CancellationToken.None);

        Assert.Equal([1, 2], booking.TableIds.OrderBy(x => x).ToList());
    }

    [Fact]
    public async Task CreatePublicAsync_WhenAllVenueTablesAvailable_CreatesPendingDepositWithoutApproval()
    {
        await using var db = CreateDb();
        await SeedBookingBasicsAsync(db, tableCount: 5);
        var request = NewCreateRequest(tableId: 1);
        request.TableIds = [1, 2, 3, 4, 5];

        var booking = await CreateService(db).CreatePublicAsync(request, CancellationToken.None);

        Assert.Equal(BookingStatuses.PendingDeposit, booking.Status);
        Assert.False(booking.RequiresApproval);
        Assert.NotNull(booking.HoldExpiresAtUtc);
        Assert.Equal([1, 2, 3, 4, 5], booking.TableIds.OrderBy(x => x).ToList());
    }

    [Fact]
    public async Task ApproveAsync_WhenPendingApproval_MovesToPendingDeposit()
    {
        await using var db = CreateDb();
        await SeedBookingBasicsAsync(db, tableCount: 5);
        var booking = NewBooking(1, BookingStatuses.PendingApproval, DateTime.UtcNow.Date.AddDays(1).AddHours(10), DateTime.UtcNow.Date.AddDays(1).AddHours(11));
        booking.RequiresApproval = true;
        booking.EstimatedAmount = 200000;
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var approved = await CreateService(db).ApproveAsync(booking.BookingId, 1, CancellationToken.None);

        Assert.Equal(BookingStatuses.PendingDeposit, approved.Status);
        Assert.False(approved.RequiresApproval);
        Assert.NotNull(approved.ApprovedAtUtc);
    }

    private static Booking NewBooking(long id, int status, DateTime start, DateTime end) => new()
    {
        BookingId = id,
        BookingCode = $"BK{id}",
        CustomerId = 1,
        StartTimeUtc = start,
        EndTimeUtc = end,
        NumberOfGuests = 2,
        Status = status
    };

    private static BookingService CreateService(PoolHubDbContext db) =>
        new(db, new TestEmailService(), new Microsoft.Extensions.Logging.Abstractions.NullLogger<BookingService>());

    private static PoolHubDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PoolHubDbContext(options);
    }

    private static async Task SeedBookingBasicsAsync(PoolHubDbContext db, int tableCount)
    {
        db.Customers.Add(new Customer { CustomerId = 1, FullName = "Customer", PhoneNumber = "0900000001", Status = true });
        db.TableTypes.Add(new TableType { TableTypeId = 1, Name = "Standard" });
        db.PricingPlans.Add(new PricingPlan { PricingPlanId = 1, Name = "Default", IsActive = true, IsDefault = true, StartsAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) });
        db.PricingPlanRules.Add(new PricingPlanRule { PricingPlanRuleId = 1, PricingPlanId = 1, TableTypeId = 1, DayType = 1, StartTime = TimeSpan.Zero, EndTime = TimeSpan.FromHours(23), HourlyRate = 100000, MinimumMinutes = 30, BillingBlockMinutes = 15, IsActive = true });
        db.PaymentMethods.Add(new PaymentMethod
        {
            PaymentMethodId = 1,
            Name = "BankTransfer",
            Code = "BANK",
            Description = "{\"vietqr\":true,\"bankCode\":\"MB\",\"bankName\":\"MB Bank\",\"accountNo\":\"989420048989\",\"accountName\":\"POOLHUB\"}",
            IsActive = true
        });
        for (var i = 1; i <= tableCount; i++)
        {
            db.VenueTables.Add(new VenueTable { TableId = i, TableTypeId = 1, ZoneId = 1, TableCode = $"T{i:00}", TableName = $"Table {i:00}", Capacity = 4, IsActive = true, OperationalStatus = 1 });
        }

        await db.SaveChangesAsync();
    }

    private static CreateBookingRequest NewCreateRequest(long tableId) => new()
    {
        CustomerId = 1,
        TableId = tableId,
        TableTypeId = 1,
        StartTimeUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(1).AddHours(10), DateTimeKind.Utc),
        EndTimeUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(1).AddHours(11), DateTimeKind.Utc),
        NumberOfGuests = 2
    };

    private static UpdateBookingRequest NewUpdateRequest(List<long> tableIds) => new()
    {
        TableId = tableIds.FirstOrDefault(),
        TableIds = tableIds,
        TableTypeId = 1,
        StartTimeUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(1).AddHours(10), DateTimeKind.Utc),
        EndTimeUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(1).AddHours(11), DateTimeKind.Utc),
        NumberOfGuests = 2
    };

    private static Session NewSession(long id, int status) => new()
    {
        SessionId = id,
        SessionCode = $"SS{id}",
        Status = status,
        StartedAtUtc = DateTime.UtcNow.AddMinutes(-30),
        OpenedByUserId = 1
    };

    private static SessionTableAssignment NewAssignment(long sessionId, long tableId, DateTime? endedAtUtc = null) => new()
    {
        SessionId = sessionId,
        TableId = tableId,
        StartedAtUtc = DateTime.UtcNow.AddMinutes(-30),
        EndedAtUtc = endedAtUtc
    };

    private sealed class TestEmailService : PoolHub.Core.Interfaces.Services.IEmailService
    {
        public void EnsureConfigured() { }
        public Task SendPasswordResetAsync(string email, string resetToken, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SendBookingConfirmedAsync(string email, string customerName, string phoneNumber, string bookingCode, string tableName, DateTime startTimeUtc, DateTime endTimeUtc, int numberOfGuests, CancellationToken ct) => Task.CompletedTask;
        public Task SendBookingCancelledAsync(string email, string customerName, string phoneNumber, string bookingCode, string tableName, DateTime startTimeUtc, DateTime endTimeUtc, int numberOfGuests, string reason, CancellationToken ct) => Task.CompletedTask;
        public Task SendDepositRefundNotificationAsync(string email, string subject, string title, string message, IReadOnlyDictionary<string, string> details, string? actionUrl, string? actionText, CancellationToken ct) => Task.CompletedTask;
    }
}
