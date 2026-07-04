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

        // 2. Lấy danh sách TableId đang có Session active (Status=1=Active, EndedAtUtc=null)
        //    Dùng SessionTableAssignment để biết bàn nào đang chơi
        var activeAssignments = await db.SessionTableAssignments
            .Where(a => a.EndedAtUtc == null)
            .Join(db.Sessions.Where(s => s.Status == 1),
                a => a.SessionId, s => s.SessionId,
                (a, s) => new { a.TableId, a.SessionId })
            .ToListAsync(ct);

        // Map TableId → SessionId (lấy session đầu tiên nếu có nhiều)
        var activeSessionMap = activeAssignments
            .GroupBy(x => x.TableId)
            .ToDictionary(g => g.Key, g => g.First().SessionId);

        var now = _clock.UtcNow;
        var nextBookings = await db.Bookings
            .AsNoTracking()
            .Where(b => b.TableId.HasValue &&
                        b.EndTimeUtc > now &&
                        b.Status == BookingStatuses.Confirmed)
            .OrderBy(b => b.StartTimeUtc)
            .Select(b => new { b.TableId, b.BookingId, b.BookingCode, b.StartTimeUtc })
            .ToListAsync(ct);

        var nextBookingMap = nextBookings
            .GroupBy(x => x.TableId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

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
                                // Ưu tiên session active, rồi booking sắp tới, rồi trạng thái lưu trong DB.
                                var activeSessionId = activeSessionMap.TryGetValue(table.TableId, out var sid) ? sid : (long?)null;
                                var nextBooking = nextBookingMap.GetValueOrDefault(table.TableId);
                                var operationalStatus = !table.IsActive
                                    ? 5
                                    : activeSessionId.HasValue
                                        ? 2
                                        : nextBooking is not null && table.OperationalStatus == 1
                                            ? 3
                                            : table.OperationalStatus;

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
                                    ActiveSessionId = activeSessionId,
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
            AvailableTables = allTableItems.Count(t => t.OperationalStatus == 1),
            OccupiedTables = allTableItems.Count(t => t.OperationalStatus == 2),
            ReservedTables = allTableItems.Count(t => t.OperationalStatus == 3),
            MaintenanceTables = allTableItems.Count(t => t.OperationalStatus == 4),
            InactiveTables = allTableItems.Count(t => t.OperationalStatus == 5),
            FetchedAtUtc = now
        };
    }
}

