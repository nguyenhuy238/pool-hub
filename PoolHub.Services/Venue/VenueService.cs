using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Venue;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Time;

namespace PoolHub.Services.Venue;

public class VenueService(PoolHubDbContext db, IClock? clock = null) : IVenueService
{
    private readonly IClock _clock = clock ?? SystemClock.Instance;

    public async Task<IEnumerable<FloorDto>> GetFloorsAsync(CancellationToken ct) => await db.Floors.Select(x => new FloorDto { FloorId = x.FloorId, Name = x.Name, IsActive = x.IsActive }).ToListAsync(ct);
    public async Task<IEnumerable<ZoneDto>> GetZonesAsync(CancellationToken ct) => await db.Zones.Select(x => new ZoneDto { ZoneId = x.ZoneId, FloorId = x.FloorId, Name = x.Name, IsActive = x.IsActive }).ToListAsync(ct);
    public async Task<IEnumerable<TableTypeDto>> GetTableTypesAsync(CancellationToken ct) => await db.TableTypes.Select(x => new TableTypeDto { TableTypeId = x.TableTypeId, Name = x.Name, Code = x.Code, DefaultCapacity = x.DefaultCapacity }).ToListAsync(ct);
    public async Task<IEnumerable<VenueTableDto>> GetTablesAsync(CancellationToken ct) => await db.VenueTables.Select(x => new VenueTableDto { TableId = x.TableId, ZoneId = x.ZoneId, TableTypeId = x.TableTypeId, TableCode = x.TableCode, TableName = x.TableName, Capacity = x.Capacity, OperationalStatus = x.OperationalStatus }).ToListAsync(ct);
    public async Task<IEnumerable<PricingPlanDto>> GetPricingPlansAsync(CancellationToken ct) => await db.PricingPlans.Select(x => new PricingPlanDto { PricingPlanId = x.PricingPlanId, Name = x.Name, IsDefault = x.IsDefault, IsActive = x.IsActive }).ToListAsync(ct);
    public async Task<IEnumerable<PricingPlanRuleDto>> GetPricingPlanRulesAsync(CancellationToken ct) => await db.PricingPlanRules.Select(x => new PricingPlanRuleDto { PricingPlanRuleId = x.PricingPlanRuleId, PricingPlanId = x.PricingPlanId, TableTypeId = x.TableTypeId, DayOfWeek = x.DayOfWeek, HourlyRate = x.HourlyRate }).ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<VenueLayoutResponse> GetLayoutAsync(CancellationToken ct)
    {
        var floors = await db.Floors
            .AsNoTracking()
            .Where(f => f.IsActive)
            .OrderBy(f => f.DisplayOrder)
            .ThenBy(f => f.FloorId)
            .ToListAsync(ct);

        var zones = await db.Zones
            .AsNoTracking()
            .Where(z => z.IsActive)
            .OrderBy(z => z.DisplayOrder)
            .ThenBy(z => z.ZoneId)
            .ToListAsync(ct);

        var tables = await db.VenueTables
            .AsNoTracking()
            .OrderBy(t => t.TableCode)
            .ToListAsync(ct);

        var tableTypeNames = await db.TableTypes
            .AsNoTracking()
            .ToDictionaryAsync(x => x.TableTypeId, x => x.Name, ct);

        var activeAssignments = await db.SessionTableAssignments
            .Where(a => a.EndedAtUtc == null)
            .Join(db.Sessions.Where(s => s.Status == 1),
                a => a.SessionId, s => s.SessionId,
                (a, s) => new { a.TableId, a.SessionId, s.StartedAtUtc })
            .ToListAsync(ct);

        var activeSessionMap = activeAssignments
            .GroupBy(x => x.TableId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.StartedAtUtc).First());

        var now = _clock.UtcNow;
        var directBookingRows = await db.Bookings
            .AsNoTracking()
            .Where(b => b.TableId.HasValue &&
                        b.EndTimeUtc > now &&
                        b.Status == BookingStatuses.Confirmed)
            .OrderBy(b => b.StartTimeUtc)
            .Select(b => new BookingTableHold(b.TableId!.Value, b.BookingId, b.BookingCode, b.StartTimeUtc))
            .ToListAsync(ct);

        var multiTableBookingRows = await db.BookingTables
            .AsNoTracking()
            .Join(db.Bookings.AsNoTracking().Where(b =>
                    b.EndTimeUtc > now &&
                    b.Status == BookingStatuses.Confirmed),
                bt => bt.BookingId,
                b => b.BookingId,
                (bt, b) => new { bt.TableId, b.BookingId, b.BookingCode, b.StartTimeUtc })
            .OrderBy(x => x.StartTimeUtc)
            .Select(x => new BookingTableHold(x.TableId, x.BookingId, x.BookingCode, x.StartTimeUtc))
            .ToListAsync(ct);

        var nextBookingMap = directBookingRows
            .Concat(multiTableBookingRows)
            .GroupBy(x => x.TableId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.StartTimeUtc).First());

        var activeZoneIds = zones.Select(z => z.ZoneId).ToHashSet();
        var tablesByZone = tables
            .Where(t => activeZoneIds.Contains(t.ZoneId))
            .GroupBy(t => t.ZoneId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var zonesByFloor = zones
            .GroupBy(z => z.FloorId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var floorGroups = floors
            .Select(floor => new VenueFloorLayoutItem
            {
                FloorId = floor.FloorId,
                FloorName = floor.Name,
                Description = floor.Description,
                DisplayOrder = floor.DisplayOrder,
                Zones = zonesByFloor.GetValueOrDefault(floor.FloorId, [])
                    .Select(zone => new VenueZoneLayoutItem
                    {
                        ZoneId = zone.ZoneId,
                        ZoneName = zone.Name,
                        Description = zone.Description,
                        DisplayOrder = zone.DisplayOrder,
                        Tables = tablesByZone.GetValueOrDefault(zone.ZoneId, [])
                            .Select(table =>
                            {
                                var activeSession = activeSessionMap.GetValueOrDefault(table.TableId);
                                var nextBooking = nextBookingMap.GetValueOrDefault(table.TableId);
                                var operationalStatus = ResolveDisplayStatus(table.IsActive, table.OperationalStatus, activeSession is not null, nextBooking is not null);

                                return new VenueTableLayoutItem
                                {
                                    TableId = table.TableId,
                                    TableCode = table.TableCode,
                                    TableName = table.TableName,
                                    TableTypeId = table.TableTypeId,
                                    TableTypeName = tableTypeNames.GetValueOrDefault(table.TableTypeId, "Chưa phân loại"),
                                    Capacity = table.Capacity,
                                    OperationalStatus = operationalStatus,
                                    PositionX = table.PositionX,
                                    PositionY = table.PositionY,
                                    IsActive = table.IsActive,
                                    ActiveSessionId = activeSession?.SessionId,
                                    ActiveSessionStartedAtUtc = activeSession?.StartedAtUtc,
                                    NextBookingId = nextBooking?.BookingId,
                                    NextBookingCode = nextBooking?.BookingCode,
                                    NextBookingStartTimeUtc = nextBooking?.StartTimeUtc
                                };
                            })
                            .OrderBy(t => t.TableCode)
                            .ToList()
                    })
                    .ToList()
            })
            .ToList();

        // 4. Tính thống kê
        var allTableItems = floorGroups.SelectMany(f => f.Zones).SelectMany(z => z.Tables).ToList();

        return new VenueLayoutResponse
        {
            Floors = floorGroups,
            TotalTables = allTableItems.Count,
            AvailableTables = allTableItems.Count(t => t.OperationalStatus == TableOperationalStatuses.Available),
            OccupiedTables = allTableItems.Count(t => t.OperationalStatus == TableOperationalStatuses.InUse),
            ReservedTables = allTableItems.Count(t => t.OperationalStatus == TableOperationalStatuses.Reserved),
            MaintenanceTables = allTableItems.Count(t => t.OperationalStatus == TableOperationalStatuses.Maintenance),
            InactiveTables = allTableItems.Count(t => t.OperationalStatus == TableOperationalStatuses.Inactive),
            FetchedAtUtc = now
        };
    }

    private static int ResolveDisplayStatus(bool isActive, int storedOperationalStatus, bool hasOpenSession, bool hasUpcomingBooking)
    {
        if (!isActive || storedOperationalStatus == TableOperationalStatuses.Inactive)
            return TableOperationalStatuses.Inactive;
        if (storedOperationalStatus == TableOperationalStatuses.Maintenance)
            return TableOperationalStatuses.Maintenance;
        if (hasOpenSession)
            return TableOperationalStatuses.InUse;
        if (hasUpcomingBooking)
            return TableOperationalStatuses.Reserved;
        return TableOperationalStatuses.Available;
    }

    private sealed record BookingTableHold(long TableId, long BookingId, string BookingCode, DateTime StartTimeUtc);
}

