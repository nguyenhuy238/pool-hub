using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using PoolHub.Core.DTOs.Session;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Services.Session;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PoolHub.UnitTests;

public class SessionServiceTests
{
    [Fact]
    public async Task StartAsync_WhenTableAlreadyHasActiveSession_ThrowsConflictException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        
        using var db = new PoolHubDbContext(options);
        
        // Setup table
        db.VenueTables.Add(new VenueTable { TableId = 1, TableName = "Table 1", TableTypeId = 1, OperationalStatus = 1 });
        
        // Setup an existing active session & assignment on table 1
        var activeSession = new Session { SessionId = 1, Status = 1, StartedAtUtc = DateTime.UtcNow };
        db.Sessions.Add(activeSession);
        db.SessionTableAssignments.Add(new SessionTableAssignment
        {
            SessionId = 1,
            TableId = 1,
            StartedAtUtc = DateTime.UtcNow.AddHours(-1),
            EndedAtUtc = null
        });
        
        await db.SaveChangesAsync();

        var service = new SessionService(db);
        var request = new StartSessionRequest { TableId = 1 };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConflictException>(() => service.StartAsync(99, request, CancellationToken.None));
        Assert.Equal("Table already has an active session.", exception.Message);
    }

    [Fact]
    public async Task StartAsync_WhenTableIsNotAvailable_ThrowsBusinessRuleException()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new PoolHubDbContext(options);
        db.VenueTables.Add(new VenueTable { TableId = 1, TableName = "Table 1", TableTypeId = 1, OperationalStatus = 4, IsActive = true });
        await db.SaveChangesAsync();

        var service = new SessionService(db);
        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.StartAsync(99, new StartSessionRequest { TableId = 1 }, CancellationToken.None));

        Assert.Equal("Table is not available for a new session.", exception.Message);
    }

    [Fact]
    public async Task StartAsync_WhenCustomerIsBlocked_ThrowsBusinessRuleException()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new PoolHubDbContext(options);
        db.Customers.Add(new Customer { CustomerId = 1, FullName = "Blocked Customer", PhoneNumber = "0900000000", Status = false });
        db.VenueTables.Add(new VenueTable { TableId = 1, TableName = "Table 1", TableTypeId = 1, OperationalStatus = 1, IsActive = true });
        await db.SaveChangesAsync();

        var service = new SessionService(db);
        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.StartAsync(99, new StartSessionRequest { TableId = 1, CustomerId = 1 }, CancellationToken.None));

        Assert.Equal("Customer is blocked or inactive.", exception.Message);
    }

    [Fact]
    public async Task StartAsync_WhenConfirmedBookingIsBeforeEarlyCheckInWindowAndTableIsFree_StartsSession()
    {
        using var db = CreateSessionStartDb();
        var now = DateTime.UtcNow;
        SeedSessionStartData(db, now.AddMinutes(30), now.AddMinutes(90));
        await db.SaveChangesAsync();

        var service = new SessionService(db, new TestPosNotificationService(), BuildBookingRulesConfig(15));

        var result = await service.StartAsync(99, new StartSessionRequest { BookingId = 1 }, CancellationToken.None);

        var booking = await db.Bookings.FindAsync(1L);
        Assert.Equal(BookingStatuses.InProgress, booking!.Status);
        Assert.True(result.StartedAtUtc >= now);
        Assert.True(result.StartedAtUtc < now.AddSeconds(10));
    }

    [Fact]
    public async Task StartAsync_WhenEarlyStartWouldOverlapAnotherBooking_ThrowsConflictException()
    {
        using var db = CreateSessionStartDb();
        var now = DateTime.UtcNow;
        SeedSessionStartData(db, now.AddMinutes(30), now.AddMinutes(90));
        db.Bookings.Add(new Booking
        {
            BookingId = 2,
            BookingCode = "BK2",
            CustomerId = 1,
            TableId = 1,
            StartTimeUtc = DateTime.SpecifyKind(now.AddMinutes(10), DateTimeKind.Utc),
            EndTimeUtc = DateTime.SpecifyKind(now.AddMinutes(20), DateTimeKind.Utc),
            NumberOfGuests = 2,
            Status = BookingStatuses.Confirmed
        });
        await db.SaveChangesAsync();

        var service = new SessionService(db, new TestPosNotificationService(), BuildBookingRulesConfig(15));

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            service.StartAsync(99, new StartSessionRequest { BookingId = 1 }, CancellationToken.None));

        Assert.Equal("Khung giờ hiện tại đã có người booking bàn này, không thể mở bàn sớm.", exception.Message);
    }

    [Fact]
    public async Task StartAsync_WhenConfirmedBookingIsInsideEarlyCheckInWindow_StartsAtActualNowAndMarksBookingInProgress()
    {
        using var db = CreateSessionStartDb();
        var now = DateTime.UtcNow;
        SeedSessionStartData(db, now.AddMinutes(15), now.AddMinutes(75));
        await db.SaveChangesAsync();

        var service = new SessionService(db, new TestPosNotificationService(), BuildBookingRulesConfig(15));

        var result = await service.StartAsync(99, new StartSessionRequest { BookingId = 1 }, CancellationToken.None);

        var booking = await db.Bookings.FindAsync(1L);
        var table = await db.VenueTables.FindAsync(1L);
        Assert.Equal(BookingStatuses.InProgress, booking!.Status);
        Assert.Equal(1, table!.OperationalStatus);
        Assert.True(result.StartedAtUtc >= now);
        Assert.True(result.StartedAtUtc < now.AddSeconds(10));
    }

    [Fact]
    public async Task StartAsync_WhenBookingAlreadyHasSession_ThrowsConflictException()
    {
        using var db = CreateSessionStartDb();
        var now = DateTime.UtcNow;
        SeedSessionStartData(db, now.AddMinutes(-5), now.AddMinutes(55));
        db.Sessions.Add(new Session { SessionId = 10, SessionCode = "SS10", BookingId = 1, Status = 1, StartedAtUtc = now, OpenedByUserId = 99 });
        await db.SaveChangesAsync();

        var service = new SessionService(db, new TestPosNotificationService(), BuildBookingRulesConfig(15));

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            service.StartAsync(99, new StartSessionRequest { BookingId = 1 }, CancellationToken.None));

        Assert.Equal("Booking already has a session.", exception.Message);
    }

    [Fact]
    public async Task TransferTableAsync_WhenNewTableAlreadyHasActiveSession_ThrowsConflictException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        
        using var db = new PoolHubDbContext(options);
        
        // Setup tables
        db.VenueTables.Add(new VenueTable { TableId = 1, TableName = "Table 1", TableTypeId = 1, OperationalStatus = 2 });
        db.VenueTables.Add(new VenueTable { TableId = 2, TableName = "Table 2", TableTypeId = 1, OperationalStatus = 2 });
        
        // Session 1 on Table 1
        var session1 = new Session { SessionId = 1, Status = 1, StartedAtUtc = DateTime.UtcNow.AddHours(-1) };
        db.Sessions.Add(session1);
        db.SessionTableAssignments.Add(new SessionTableAssignment { SessionId = 1, TableId = 1, StartedAtUtc = session1.StartedAtUtc });

        // Session 2 active on Table 2 (the target table)
        var session2 = new Session { SessionId = 2, Status = 1, StartedAtUtc = DateTime.UtcNow.AddHours(-1) };
        db.Sessions.Add(session2);
        db.SessionTableAssignments.Add(new SessionTableAssignment { SessionId = 2, TableId = 2, StartedAtUtc = session2.StartedAtUtc });
        
        await db.SaveChangesAsync();

        var service = new SessionService(db);

        // Act & Assert: Transfer Session 1 to Table 2 (which is active under Session 2)
        var sourceAssignmentId = await db.SessionTableAssignments
            .Where(x => x.SessionId == 1 && x.TableId == 1)
            .Select(x => x.SessionTableAssignmentId)
            .FirstAsync();
        var exception = await Assert.ThrowsAsync<ConflictException>(() => service.TransferTableAsync(1, new TransferTableRequest { SourceAssignmentId = sourceAssignmentId, ToTableId = 2 }, 99, CancellationToken.None));
        Assert.Equal("New table already has an active session.", exception.Message);
    }

    [Fact]
    public async Task GetActiveSessionsAsync_ReturnsTotalDurationAcrossAssignments()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new PoolHubDbContext(options);
        var now = DateTime.UtcNow;
        db.Floors.Add(new Floor { FloorId = 1, Name = "Floor 1", IsActive = true });
        db.Zones.Add(new Zone { ZoneId = 1, FloorId = 1, Name = "Zone 1", IsActive = true });
        db.VenueTables.Add(new VenueTable { TableId = 1, ZoneId = 1, TableCode = "T1", TableName = "Table 1", TableTypeId = 1, OperationalStatus = 1, IsActive = true });
        db.VenueTables.Add(new VenueTable { TableId = 2, ZoneId = 1, TableCode = "T2", TableName = "Table 2", TableTypeId = 1, OperationalStatus = 2, IsActive = true });
        db.Sessions.Add(new Session { SessionId = 1, SessionCode = "SS1", Status = 1, StartedAtUtc = now.AddMinutes(-35), OpenedByUserId = 99 });
        db.SessionTableAssignments.Add(new SessionTableAssignment
        {
            SessionId = 1,
            TableId = 1,
            StartedAtUtc = now.AddMinutes(-35),
            EndedAtUtc = now.AddMinutes(-5),
            DurationMinutes = 30
        });
        db.SessionTableAssignments.Add(new SessionTableAssignment
        {
            SessionId = 1,
            TableId = 2,
            StartedAtUtc = now.AddMinutes(-5),
            EndedAtUtc = null
        });
        await db.SaveChangesAsync();

        var service = new SessionService(db);

        var result = await service.GetActiveSessionsAsync(null, null, null, CancellationToken.None);

        Assert.Single(result);
        Assert.True(result[0].DurationMinutes >= 35);
        Assert.Single(result[0].ActiveAssignments);
        Assert.Single(result[0].ReleasedAssignments);
        Assert.Equal(1, result[0].ActiveTableCount);
        Assert.Equal(1, result[0].ReleasedTableCount);
        Assert.Equal(2, result[0].ActiveAssignments[0].TableId);
        Assert.Equal(1, result[0].ReleasedAssignments[0].TableId);
    }

    [Fact]
    public async Task GetSummaryAsync_WhenLegacyMinimumSixtyMinutesExists_UsesActualMinutes()
    {
        using var db = CreateDb();
        var startedAt = new DateTime(2026, 6, 8, 10, 0, 0, DateTimeKind.Utc);
        SeedPricing(db, startedAt, hourlyRate: 25000, minimumMinutes: 60, billingBlockMinutes: 15);
        db.Floors.Add(new Floor { FloorId = 1, Name = "Floor 1" });
        db.Zones.Add(new Zone { ZoneId = 1, FloorId = 1, Name = "Zone 1" });
        db.VenueTables.Add(new VenueTable { TableId = 1, ZoneId = 1, TableCode = "T1", TableName = "Table 1", TableTypeId = 1, OperationalStatus = 2, IsActive = true });
        db.Sessions.Add(new Session { SessionId = 1, SessionCode = "SS1", Status = 2, StartedAtUtc = startedAt, EndedAtUtc = startedAt.AddMinutes(1), OpenedByUserId = 99 });
        db.SessionTableAssignments.Add(new SessionTableAssignment { SessionId = 1, TableId = 1, StartedAtUtc = startedAt, EndedAtUtc = startedAt.AddMinutes(1) });
        await db.SaveChangesAsync();

        var summary = await new SessionService(db).GetSummaryAsync(1, CancellationToken.None);

        Assert.Equal(1, summary.CurrentDurationMinutes);
        Assert.Equal(1, summary.Assignments[0].BillableDurationMinutes);
        Assert.Equal(416.67m, summary.TimeSubtotalAmount);
    }

    [Fact]
    public async Task GetSummaryAsync_WhenLegacyBillingBlockThirtyExists_UsesActualMinutes()
    {
        using var db = CreateDb();
        var startedAt = new DateTime(2026, 6, 8, 10, 0, 0, DateTimeKind.Utc);
        SeedPricing(db, startedAt, hourlyRate: 60000, minimumMinutes: 30, billingBlockMinutes: 30);
        db.Floors.Add(new Floor { FloorId = 1, Name = "Floor 1" });
        db.Zones.Add(new Zone { ZoneId = 1, FloorId = 1, Name = "Zone 1" });
        db.VenueTables.Add(new VenueTable { TableId = 1, ZoneId = 1, TableCode = "T1", TableName = "Table 1", TableTypeId = 1, OperationalStatus = 2, IsActive = true });
        db.Sessions.Add(new Session { SessionId = 1, SessionCode = "SS1", Status = 2, StartedAtUtc = startedAt, EndedAtUtc = startedAt.AddMinutes(31), OpenedByUserId = 99 });
        db.SessionTableAssignments.Add(new SessionTableAssignment { SessionId = 1, TableId = 1, StartedAtUtc = startedAt, EndedAtUtc = startedAt.AddMinutes(31) });
        await db.SaveChangesAsync();

        var summary = await new SessionService(db).GetSummaryAsync(1, CancellationToken.None);

        Assert.Equal(31, summary.Assignments[0].ActualDurationMinutes);
        Assert.Equal(31, summary.Assignments[0].BillableDurationMinutes);
        Assert.Equal(31000, summary.TimeSubtotalAmount);
    }

    [Fact]
    public async Task GetSummaryAsync_WhenMultipleAssignments_SumsEachAssignmentAmount()
    {
        using var db = CreateDb();
        var startedAt = new DateTime(2026, 6, 8, 10, 0, 0, DateTimeKind.Utc);
        SeedPricing(db, startedAt, hourlyRate: 60000, minimumMinutes: 30, billingBlockMinutes: 30);
        db.Floors.Add(new Floor { FloorId = 1, Name = "Floor 1" });
        db.Zones.Add(new Zone { ZoneId = 1, FloorId = 1, Name = "Zone 1" });
        db.VenueTables.Add(new VenueTable { TableId = 1, ZoneId = 1, TableCode = "T1", TableName = "Table 1", TableTypeId = 1, OperationalStatus = 1, IsActive = true });
        db.VenueTables.Add(new VenueTable { TableId = 2, ZoneId = 1, TableCode = "T2", TableName = "Table 2", TableTypeId = 1, OperationalStatus = 2, IsActive = true });
        db.Sessions.Add(new Session { SessionId = 1, SessionCode = "SS1", Status = 2, StartedAtUtc = startedAt, EndedAtUtc = startedAt.AddMinutes(61), OpenedByUserId = 99 });
        db.SessionTableAssignments.Add(new SessionTableAssignment { SessionId = 1, TableId = 1, StartedAtUtc = startedAt, EndedAtUtc = startedAt.AddMinutes(31) });
        db.SessionTableAssignments.Add(new SessionTableAssignment { SessionId = 1, TableId = 2, StartedAtUtc = startedAt.AddMinutes(31), EndedAtUtc = startedAt.AddMinutes(61) });
        await db.SaveChangesAsync();

        var summary = await new SessionService(db).GetSummaryAsync(1, CancellationToken.None);

        Assert.Equal(2, summary.Assignments.Count);
        Assert.Equal(61000, summary.TimeSubtotalAmount);
    }

    [Fact]
    public async Task GetSummaryAsync_WhenTransferredMultipleTimes_UsesActualMinutesPerAssignment()
    {
        using var db = CreateDb();
        var startedAt = new DateTime(2026, 6, 8, 10, 0, 0, DateTimeKind.Utc);
        SeedPricing(db, startedAt, hourlyRate: 60000, minimumMinutes: 30, billingBlockMinutes: 15);
        db.Floors.Add(new Floor { FloorId = 1, Name = "Floor 1" });
        db.Zones.Add(new Zone { ZoneId = 1, FloorId = 1, Name = "Zone 1" });
        db.VenueTables.Add(new VenueTable { TableId = 1, ZoneId = 1, TableCode = "A02", TableName = "A02", TableTypeId = 1, OperationalStatus = 1, IsActive = true });
        db.VenueTables.Add(new VenueTable { TableId = 2, ZoneId = 1, TableCode = "A01", TableName = "A01", TableTypeId = 1, OperationalStatus = 1, IsActive = true });
        db.Sessions.Add(new Session { SessionId = 1, SessionCode = "SS1", Status = 2, StartedAtUtc = startedAt, EndedAtUtc = startedAt.AddMinutes(31), OpenedByUserId = 99 });
        db.SessionTableAssignments.Add(new SessionTableAssignment { SessionId = 1, TableId = 1, StartedAtUtc = startedAt, EndedAtUtc = startedAt.AddMinutes(29) });
        db.SessionTableAssignments.Add(new SessionTableAssignment { SessionId = 1, TableId = 2, StartedAtUtc = startedAt.AddMinutes(29), EndedAtUtc = startedAt.AddMinutes(30) });
        db.SessionTableAssignments.Add(new SessionTableAssignment { SessionId = 1, TableId = 1, StartedAtUtc = startedAt.AddMinutes(30), EndedAtUtc = startedAt.AddMinutes(31) });
        await db.SaveChangesAsync();

        var summary = await new SessionService(db).GetSummaryAsync(1, CancellationToken.None);

        Assert.Equal(31, summary.ActualDurationMinutes);
        Assert.Equal(31, summary.BillableDurationMinutes);
        Assert.Equal(31, summary.Assignments.Sum(x => x.BillableDurationMinutes));
        Assert.Equal(31000, summary.TimeSubtotalAmount);
    }

    [Fact]
    public async Task GetSummaryAsync_WhenPlayedThreeMinutesWithLegacyMinimumThirty_ChargesThreeMinutes()
    {
        using var db = CreateDb();
        var startedAt = new DateTime(2026, 6, 8, 10, 0, 0, DateTimeKind.Utc);
        SeedPricing(db, startedAt, hourlyRate: 60000, minimumMinutes: 30, billingBlockMinutes: 15);
        db.Floors.Add(new Floor { FloorId = 1, Name = "Floor 1" });
        db.Zones.Add(new Zone { ZoneId = 1, FloorId = 1, Name = "Zone 1" });
        db.VenueTables.Add(new VenueTable { TableId = 1, ZoneId = 1, TableCode = "T1", TableName = "Table 1", TableTypeId = 1, OperationalStatus = 2, IsActive = true });
        db.Sessions.Add(new Session { SessionId = 1, SessionCode = "SS1", Status = 2, StartedAtUtc = startedAt, EndedAtUtc = startedAt.AddMinutes(3), OpenedByUserId = 99 });
        db.SessionTableAssignments.Add(new SessionTableAssignment { SessionId = 1, TableId = 1, StartedAtUtc = startedAt, EndedAtUtc = startedAt.AddMinutes(3) });
        await db.SaveChangesAsync();

        var summary = await new SessionService(db).GetSummaryAsync(1, CancellationToken.None);

        Assert.Equal(3, summary.Assignments[0].BillableDurationMinutes);
        Assert.Equal(3000, summary.TimeSubtotalAmount);
    }

    [Fact]
    public async Task GetSummaryAsync_WhenTransferredToDifferentRateTable_ChargesEachAssignmentIndependently()
    {
        using var db = CreateDb();
        var startedAt = new DateTime(2026, 6, 8, 10, 0, 0, DateTimeKind.Utc);
        db.PricingPlans.Add(new PricingPlan { PricingPlanId = 1, Name = "Default Plan", IsDefault = true, IsActive = true, StartsAtUtc = startedAt.AddDays(-1) });
        db.PricingPlanRules.Add(new PricingPlanRule { PricingPlanRuleId = 1, PricingPlanId = 1, TableTypeId = 1, DayType = 1, StartTime = TimeSpan.Zero, EndTime = new TimeSpan(23, 59, 59), HourlyRate = 60000, MinimumMinutes = 30, BillingBlockMinutes = 15, IsActive = true });
        db.PricingPlanRules.Add(new PricingPlanRule { PricingPlanRuleId = 2, PricingPlanId = 1, TableTypeId = 2, DayType = 1, StartTime = TimeSpan.Zero, EndTime = new TimeSpan(23, 59, 59), HourlyRate = 90000, MinimumMinutes = 30, BillingBlockMinutes = 15, IsActive = true });
        db.Floors.Add(new Floor { FloorId = 1, Name = "Floor 1" });
        db.Zones.Add(new Zone { ZoneId = 1, FloorId = 1, Name = "Zone 1" });
        db.VenueTables.Add(new VenueTable { TableId = 1, ZoneId = 1, TableCode = "A", TableName = "Table A", TableTypeId = 1, OperationalStatus = 1, IsActive = true });
        db.VenueTables.Add(new VenueTable { TableId = 2, ZoneId = 1, TableCode = "B", TableName = "Table B", TableTypeId = 2, OperationalStatus = 2, IsActive = true });
        db.Sessions.Add(new Session { SessionId = 1, SessionCode = "SS1", Status = 2, StartedAtUtc = startedAt, EndedAtUtc = startedAt.AddMinutes(30), OpenedByUserId = 99 });
        db.SessionTableAssignments.Add(new SessionTableAssignment { SessionId = 1, TableId = 1, StartedAtUtc = startedAt, EndedAtUtc = startedAt.AddMinutes(10) });
        db.SessionTableAssignments.Add(new SessionTableAssignment { SessionId = 1, TableId = 2, StartedAtUtc = startedAt.AddMinutes(10), EndedAtUtc = startedAt.AddMinutes(30) });
        await db.SaveChangesAsync();

        var summary = await new SessionService(db).GetSummaryAsync(1, CancellationToken.None);

        Assert.Equal(10000, summary.Assignments.Single(x => x.TableId == 1).Amount);
        Assert.Equal(30000, summary.Assignments.Single(x => x.TableId == 2).Amount);
        Assert.Equal(40000, summary.TimeSubtotalAmount);
    }

    [Fact]
    public async Task CloseWithSummaryAsync_WhenDepositExceedsGrandTotal_MarksInvoicePaidAndTracksRefundableExcess()
    {
        using var db = CreateDb();
        var startedAt = new DateTime(2026, 6, 8, 10, 0, 0, DateTimeKind.Utc);
        SeedSessionWithBookingDeposit(db, startedAt, durationMinutes: 150, depositPaidAmount: 200000, depositStatus: BookingDepositStatuses.Paid);
        await db.SaveChangesAsync();

        var response = await new SessionService(db).CloseWithSummaryAsync(1, 99, new CloseSessionRequest { EndedAtUtc = startedAt.AddMinutes(150), GenerateInvoice = true }, CancellationToken.None);

        var invoice = await db.Invoices.SingleAsync();
        var deposit = await db.BookingDeposits.SingleAsync();
        Assert.True(response.InvoiceGenerated);
        Assert.Equal(150000, invoice.GrandTotalAmount);
        Assert.Equal(150000, invoice.PaidAmount);
        Assert.Equal(InvoicePaymentStatuses.Paid, invoice.PaymentStatus);
        Assert.Equal(150000, deposit.AppliedAmount);
        Assert.Equal(0, deposit.RefundedAmount);
        Assert.Equal(BookingDepositStatuses.AppliedToInvoice, deposit.Status);
        var refund = await db.BookingDepositRefunds.SingleAsync();
        Assert.Equal(50000, refund.Amount);
        Assert.Equal(BookingDepositRefundReasons.DepositExcess, refund.Reason);
        Assert.Equal(BookingDepositRefundStatuses.PendingCustomerInfo, refund.Status);
    }

    [Fact]
    public async Task CloseWithSummaryAsync_WhenDepositIsLessThanGrandTotal_MarksInvoicePartiallyPaid()
    {
        using var db = CreateDb();
        var startedAt = new DateTime(2026, 6, 8, 10, 0, 0, DateTimeKind.Utc);
        SeedSessionWithBookingDeposit(db, startedAt, durationMinutes: 250, depositPaidAmount: 200000, depositStatus: BookingDepositStatuses.Paid);
        await db.SaveChangesAsync();

        await new SessionService(db).CloseWithSummaryAsync(1, 99, new CloseSessionRequest { EndedAtUtc = startedAt.AddMinutes(250), GenerateInvoice = true }, CancellationToken.None);

        var invoice = await db.Invoices.SingleAsync();
        var deposit = await db.BookingDeposits.SingleAsync();
        Assert.Equal(250000, invoice.GrandTotalAmount);
        Assert.Equal(200000, invoice.PaidAmount);
        Assert.Equal(InvoicePaymentStatuses.PartiallyPaid, invoice.PaymentStatus);
        Assert.Equal(200000, deposit.AppliedAmount);
        Assert.Equal(0, deposit.RefundedAmount);
        Assert.Equal(BookingDepositStatuses.AppliedToInvoice, deposit.Status);
    }

    [Fact]
    public async Task CloseWithSummaryAsync_WhenDepositEqualsGrandTotal_MarksInvoicePaidWithoutRefund()
    {
        using var db = CreateDb();
        var startedAt = new DateTime(2026, 6, 8, 10, 0, 0, DateTimeKind.Utc);
        SeedSessionWithBookingDeposit(db, startedAt, durationMinutes: 200, depositPaidAmount: 200000, depositStatus: BookingDepositStatuses.Paid);
        await db.SaveChangesAsync();

        await new SessionService(db).CloseWithSummaryAsync(1, 99, new CloseSessionRequest { EndedAtUtc = startedAt.AddMinutes(200), GenerateInvoice = true }, CancellationToken.None);

        var invoice = await db.Invoices.SingleAsync();
        var deposit = await db.BookingDeposits.SingleAsync();
        Assert.Equal(200000, invoice.GrandTotalAmount);
        Assert.Equal(200000, invoice.PaidAmount);
        Assert.Equal(InvoicePaymentStatuses.Paid, invoice.PaymentStatus);
        Assert.Equal(200000, deposit.AppliedAmount);
        Assert.Equal(0, deposit.RefundedAmount);
        Assert.Empty(await db.BookingDepositRefunds.ToListAsync());
    }

    [Fact]
    public async Task CloseWithSummaryAsync_WhenDepositIsNotPaid_DoesNotApplyDeposit()
    {
        using var db = CreateDb();
        var startedAt = new DateTime(2026, 6, 8, 10, 0, 0, DateTimeKind.Utc);
        SeedSessionWithBookingDeposit(db, startedAt, durationMinutes: 30, depositPaidAmount: 200000, depositStatus: BookingDepositStatuses.Pending);
        await db.SaveChangesAsync();

        await new SessionService(db).CloseWithSummaryAsync(1, 99, new CloseSessionRequest { EndedAtUtc = startedAt.AddMinutes(30), GenerateInvoice = true }, CancellationToken.None);

        var invoice = await db.Invoices.SingleAsync();
        var deposit = await db.BookingDeposits.SingleAsync();
        Assert.Equal(30000, invoice.GrandTotalAmount);
        Assert.Equal(0, invoice.PaidAmount);
        Assert.Equal(InvoicePaymentStatuses.Unpaid, invoice.PaymentStatus);
        Assert.Equal(0, deposit.AppliedAmount);
    }

    [Fact]
    public async Task CloseWithSummaryAsync_WhenCalledTwice_DoesNotCreateSecondInvoiceOrApplyDepositTwice()
    {
        using var db = CreateDb();
        var startedAt = new DateTime(2026, 6, 8, 10, 0, 0, DateTimeKind.Utc);
        SeedSessionWithBookingDeposit(db, startedAt, durationMinutes: 60, depositPaidAmount: 30000, depositStatus: BookingDepositStatuses.Paid);
        await db.SaveChangesAsync();
        var service = new SessionService(db);

        await service.CloseWithSummaryAsync(1, 99, new CloseSessionRequest { EndedAtUtc = startedAt.AddMinutes(60), GenerateInvoice = true }, CancellationToken.None);
        await service.CloseWithSummaryAsync(1, 99, new CloseSessionRequest { EndedAtUtc = startedAt.AddMinutes(60), GenerateInvoice = true }, CancellationToken.None);

        var invoice = await db.Invoices.SingleAsync();
        Assert.Equal(30000, invoice.PaidAmount);
        Assert.Equal(1, await db.Payments.CountAsync());
        Assert.Empty(await db.BookingDepositRefunds.ToListAsync());
    }

    [Fact]
    public async Task CloseWithSummaryAsync_WhenCalledTwiceWithExcessDeposit_DoesNotCreateDuplicateRefund()
    {
        using var db = CreateDb();
        var startedAt = new DateTime(2026, 6, 8, 10, 0, 0, DateTimeKind.Utc);
        SeedSessionWithBookingDeposit(db, startedAt, durationMinutes: 150, depositPaidAmount: 200000, depositStatus: BookingDepositStatuses.Paid);
        await db.SaveChangesAsync();
        var service = new SessionService(db);

        await service.CloseWithSummaryAsync(1, 99, new CloseSessionRequest { EndedAtUtc = startedAt.AddMinutes(150), GenerateInvoice = true }, CancellationToken.None);
        await service.CloseWithSummaryAsync(1, 99, new CloseSessionRequest { EndedAtUtc = startedAt.AddMinutes(150), GenerateInvoice = true }, CancellationToken.None);

        var invoice = await db.Invoices.SingleAsync();
        var deposit = await db.BookingDeposits.SingleAsync();
        Assert.Equal(150000, invoice.PaidAmount);
        Assert.Equal(150000, deposit.AppliedAmount);
        Assert.Equal(0, deposit.RefundedAmount);
        Assert.Equal(1, await db.Payments.CountAsync());
        Assert.Equal(1, await db.BookingDepositRefunds.CountAsync());
    }

    [Fact]
    public async Task GetSummaryAsync_WhenPricingRuleMissing_ThrowsConflictException()
    {
        using var db = CreateDb();
        var startedAt = new DateTime(2026, 6, 8, 10, 0, 0, DateTimeKind.Utc);
        db.Floors.Add(new Floor { FloorId = 1, Name = "Floor 1" });
        db.Zones.Add(new Zone { ZoneId = 1, FloorId = 1, Name = "Zone 1" });
        db.VenueTables.Add(new VenueTable { TableId = 1, ZoneId = 1, TableCode = "T1", TableName = "Table 1", TableTypeId = 1, OperationalStatus = 2, IsActive = true });
        db.Sessions.Add(new Session { SessionId = 1, SessionCode = "SS1", Status = 2, StartedAtUtc = startedAt, EndedAtUtc = startedAt.AddMinutes(10), OpenedByUserId = 99 });
        db.SessionTableAssignments.Add(new SessionTableAssignment { SessionId = 1, TableId = 1, StartedAtUtc = startedAt, EndedAtUtc = startedAt.AddMinutes(10) });
        await db.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ConflictException>(() => new SessionService(db).GetSummaryAsync(1, CancellationToken.None));
        Assert.Contains(exception.Errors, error => error == "tableCode=T1");
        Assert.Contains(exception.Errors, error => error.StartsWith("venueLocalTime=", StringComparison.Ordinal));
    }

    private static PoolHubDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PoolHubDbContext(options);
    }

    private static PoolHubDbContext CreateSessionStartDb()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new PoolHubDbContext(options);
    }

    private static void SeedSessionStartData(PoolHubDbContext db, DateTime startUtc, DateTime endUtc)
    {
        db.Customers.Add(new Customer { CustomerId = 1, FullName = "Test Customer", PhoneNumber = "0900000000", Status = true });
        db.VenueTables.Add(new VenueTable { TableId = 1, TableCode = "T1", TableName = "Table 1", TableTypeId = 1, OperationalStatus = 1, IsActive = true });
        db.Bookings.Add(new Booking
        {
            BookingId = 1,
            BookingCode = "BK1",
            CustomerId = 1,
            TableId = 1,
            StartTimeUtc = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc),
            EndTimeUtc = DateTime.SpecifyKind(endUtc, DateTimeKind.Utc),
            NumberOfGuests = 2,
            Status = BookingStatuses.Confirmed
        });
        SeedPricing(db, startUtc, hourlyRate: 60000, minimumMinutes: 30, billingBlockMinutes: 15);
    }

    private static void SeedSessionWithBookingDeposit(PoolHubDbContext db, DateTime startedAt, int durationMinutes, decimal depositPaidAmount, int depositStatus)
    {
        SeedPricing(db, startedAt, hourlyRate: 60000, minimumMinutes: 30, billingBlockMinutes: 15);
        db.Customers.Add(new Customer { CustomerId = 1, FullName = "Deposit Customer", PhoneNumber = "0900000000", Status = true });
        db.Floors.Add(new Floor { FloorId = 1, Name = "Floor 1" });
        db.Zones.Add(new Zone { ZoneId = 1, FloorId = 1, Name = "Zone 1" });
        db.VenueTables.Add(new VenueTable { TableId = 1, ZoneId = 1, TableCode = "T1", TableName = "Table 1", TableTypeId = 1, OperationalStatus = 2, IsActive = true });
        db.Bookings.Add(new Booking
        {
            BookingId = 1,
            BookingCode = "BK1",
            CustomerId = 1,
            TableId = 1,
            StartTimeUtc = startedAt,
            EndTimeUtc = startedAt.AddMinutes(durationMinutes),
            NumberOfGuests = 2,
            Status = BookingStatuses.InProgress
        });
        db.BookingDeposits.Add(new BookingDeposit
        {
            BookingDepositId = 1,
            BookingId = 1,
            RequiredAmount = depositPaidAmount,
            PaidAmount = depositPaidAmount,
            Status = depositStatus,
            PaidAtUtc = depositStatus == BookingDepositStatuses.Paid ? startedAt.AddMinutes(-5) : null,
            DueAtUtc = startedAt.AddHours(-1)
        });
        db.Sessions.Add(new Session { SessionId = 1, SessionCode = "SS1", BookingId = 1, CustomerId = 1, Status = 1, StartedAtUtc = startedAt, OpenedByUserId = 99 });
        db.SessionTableAssignments.Add(new SessionTableAssignment { SessionId = 1, TableId = 1, StartedAtUtc = startedAt });
    }

    private static IConfiguration BuildBookingRulesConfig(int earlyCheckInMinutes) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BookingRules:EarlyCheckInMinutes"] = earlyCheckInMinutes.ToString()
            })
            .Build();

    private static void SeedPricing(PoolHubDbContext db, DateTime startedAt, decimal hourlyRate, int minimumMinutes, int billingBlockMinutes)
    {
        db.PricingPlans.Add(new PricingPlan { PricingPlanId = 1, Name = "Default Plan", IsDefault = true, IsActive = true, StartsAtUtc = startedAt.AddDays(-1) });
        db.PricingPlanRules.Add(new PricingPlanRule
        {
            PricingPlanRuleId = 1,
            PricingPlanId = 1,
            TableTypeId = 1,
            DayType = 1,
            StartTime = TimeSpan.Zero,
            EndTime = new TimeSpan(23, 59, 59),
            HourlyRate = hourlyRate,
            MinimumMinutes = minimumMinutes,
            BillingBlockMinutes = billingBlockMinutes,
            IsActive = true
        });
    }

    private sealed class TestPosNotificationService : IPosNotificationService
    {
        public Task NotifyTableUpdateAsync(int tableId, CancellationToken ct = default) => Task.CompletedTask;
        public Task NotifyBookingUpdateAsync(int bookingId, CancellationToken ct = default) => Task.CompletedTask;
        public Task NotifySessionUpdateAsync(int sessionId, CancellationToken ct = default) => Task.CompletedTask;
        public Task NotifyRefreshPosAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task NotifySessionStartedAsync(int sessionId, int tableId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
