using Microsoft.EntityFrameworkCore;
using PoolHub.Core.Entities;
using PoolHub.Infrastructure.Data;
using PoolHub.Services.Venue;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Time;

namespace PoolHub.UnitTests;

public class VenueServiceTests
{
    [Fact]
    public async Task GetLayoutAsync_DerivesDisplayStatusesWithoutMutatingOperationalStatus()
    {
        var now = new DateTime(2026, 7, 17, 10, 0, 0, DateTimeKind.Utc);
        await using var db = CreateDb();
        SeedVenue(db);

        db.VenueTables.AddRange(
            NewTable(1, "T01", TableOperationalStatuses.Available),
            NewTable(2, "T02", TableOperationalStatuses.Available),
            NewTable(3, "T03", TableOperationalStatuses.Maintenance),
            NewTable(4, "T04", TableOperationalStatuses.Available, isActive: false),
            NewTable(5, "T05", TableOperationalStatuses.Available),
            NewTable(6, "T06", TableOperationalStatuses.Available));
        db.Sessions.AddRange(
            new Session { SessionId = 1, SessionCode = "SS1", Status = 1, StartedAtUtc = now.AddHours(-1), OpenedByUserId = 1 },
            new Session { SessionId = 2, SessionCode = "SS2", Status = 1, StartedAtUtc = now.AddHours(-2), OpenedByUserId = 1 });
        db.SessionTableAssignments.AddRange(
            new SessionTableAssignment { SessionTableAssignmentId = 1, SessionId = 1, TableId = 1, StartedAtUtc = now.AddHours(-1) },
            new SessionTableAssignment { SessionTableAssignmentId = 2, SessionId = 2, TableId = 3, StartedAtUtc = now.AddHours(-2) });
        db.Bookings.AddRange(
            NewBooking(1, "BK1", tableId: 2, start: now.AddMinutes(30), end: now.AddHours(2)),
            NewBooking(2, "BK2", tableId: null, start: now.AddMinutes(15), end: now.AddHours(1)));
        db.BookingTables.Add(new BookingTable { BookingTableId = 1, BookingId = 2, TableId = 6 });
        await db.SaveChangesAsync();

        var layout = await new VenueService(db, new FixedClock(now)).GetLayoutAsync(CancellationToken.None);
        var tables = layout.Floors.SelectMany(f => f.Zones).SelectMany(z => z.Tables).ToDictionary(t => t.TableCode);

        Assert.Equal(TableOperationalStatuses.InUse, tables["T01"].OperationalStatus);
        Assert.Equal(now.AddHours(-1), tables["T01"].ActiveSessionStartedAtUtc);
        Assert.Equal(TableOperationalStatuses.Reserved, tables["T02"].OperationalStatus);
        Assert.Equal(TableOperationalStatuses.Maintenance, tables["T03"].OperationalStatus);
        Assert.Equal(TableOperationalStatuses.Inactive, tables["T04"].OperationalStatus);
        Assert.Equal(TableOperationalStatuses.Available, tables["T05"].OperationalStatus);
        Assert.Equal(TableOperationalStatuses.Reserved, tables["T06"].OperationalStatus);
        Assert.Equal(1, layout.AvailableTables);
        Assert.Equal(1, layout.OccupiedTables);
        Assert.Equal(2, layout.ReservedTables);
        Assert.Equal(1, layout.MaintenanceTables);
        Assert.Equal(1, layout.InactiveTables);

        Assert.Equal(TableOperationalStatuses.Available, (await db.VenueTables.FindAsync(1L))!.OperationalStatus);
        Assert.Equal(TableOperationalStatuses.Maintenance, (await db.VenueTables.FindAsync(3L))!.OperationalStatus);
    }

    private static PoolHubDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PoolHubDbContext(options);
    }

    private static void SeedVenue(PoolHubDbContext db)
    {
        db.Floors.Add(new Floor { FloorId = 1, Name = "Floor 1", DisplayOrder = 1, IsActive = true });
        db.Zones.Add(new Zone { ZoneId = 1, FloorId = 1, Name = "Zone A", DisplayOrder = 1, IsActive = true });
        db.TableTypes.Add(new TableType { TableTypeId = 1, Name = "Standard", Code = "STD", DefaultCapacity = 4, IsActive = true });
    }

    private static VenueTable NewTable(long id, string code, int status, bool isActive = true) => new()
    {
        TableId = id,
        ZoneId = 1,
        TableTypeId = 1,
        TableCode = code,
        TableName = $"Table {code}",
        Capacity = 4,
        OperationalStatus = status,
        IsActive = isActive
    };

    private static Booking NewBooking(long id, string code, long? tableId, DateTime start, DateTime end) => new()
    {
        BookingId = id,
        BookingCode = code,
        CustomerId = 1,
        TableId = tableId,
        StartTimeUtc = start,
        EndTimeUtc = end,
        NumberOfGuests = 2,
        Status = BookingStatuses.Confirmed
    };

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
        public DateTimeOffset UtcNowOffset => new(UtcNow, TimeSpan.Zero);
    }
}
