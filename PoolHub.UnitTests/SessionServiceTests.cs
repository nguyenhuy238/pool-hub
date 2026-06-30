using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PoolHub.Core.DTOs.Session;
using PoolHub.Core.Entities;
using PoolHub.Services.Session;
using PoolHub.Infrastructure.Data;
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
        var exception = await Assert.ThrowsAsync<ConflictException>(() => service.TransferTableAsync(1, new TransferTableRequest { ToTableId = 2 }, 99, CancellationToken.None));
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
    }

    [Fact]
    public async Task GetSummaryAsync_WhenMinimumSixtyMinutesApplies_ReturnsBillableSixtyMinutes()
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
        Assert.Equal(60, summary.Assignments[0].BillableDurationMinutes);
        Assert.Equal(25000, summary.TimeSubtotalAmount);
    }

    [Fact]
    public async Task GetSummaryAsync_WhenBillingBlockThirtyApplies_RoundsUpToSixtyMinutes()
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
        Assert.Equal(60, summary.Assignments[0].BillableDurationMinutes);
        Assert.Equal(60000, summary.TimeSubtotalAmount);
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
        Assert.Equal(90000, summary.TimeSubtotalAmount);
    }

    [Fact]
    public async Task GetSummaryAsync_WhenTransferredMultipleTimes_AppliesMinimumAndBlockOnceForSession()
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
        Assert.Equal(45, summary.BillableDurationMinutes);
        Assert.Equal(45, summary.Assignments.Sum(x => x.BillableDurationMinutes));
        Assert.Equal(45000, summary.TimeSubtotalAmount);
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

        Assert.Contains("Không tìm thấy bảng giá", exception.Message);
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

    private static void SeedPricing(PoolHubDbContext db, DateTime startedAt, decimal hourlyRate, int minimumMinutes, int billingBlockMinutes)
    {
        db.PricingPlans.Add(new PricingPlan { PricingPlanId = 1, Name = "Default Plan", IsDefault = true, IsActive = true, StartsAtUtc = startedAt.AddDays(-1) });
        db.PricingPlanRules.Add(new PricingPlanRule
        {
            PricingPlanRuleId = 1,
            PricingPlanId = 1,
            TableTypeId = 1,
            DayOfWeek = (int)startedAt.DayOfWeek,
            StartTime = TimeSpan.Zero,
            EndTime = new TimeSpan(23, 59, 59),
            HourlyRate = hourlyRate,
            MinimumMinutes = minimumMinutes,
            BillingBlockMinutes = billingBlockMinutes,
            IsActive = true
        });
    }
}
