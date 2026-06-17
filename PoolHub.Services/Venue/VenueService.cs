using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Venue;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;

namespace PoolHub.Services.Venue;

public class VenueService(PoolHubDbContext db) : IVenueService
{
    public async Task<IEnumerable<FloorDto>> GetFloorsAsync(CancellationToken ct) => await db.Floors.Select(x => new FloorDto { FloorId = x.FloorId, Name = x.Name, IsActive = x.IsActive }).ToListAsync(ct);
    public async Task<IEnumerable<ZoneDto>> GetZonesAsync(CancellationToken ct) => await db.Zones.Select(x => new ZoneDto { ZoneId = x.ZoneId, FloorId = x.FloorId, Name = x.Name, IsActive = x.IsActive }).ToListAsync(ct);
    public async Task<IEnumerable<TableTypeDto>> GetTableTypesAsync(CancellationToken ct) => await db.TableTypes.Select(x => new TableTypeDto { TableTypeId = x.TableTypeId, Name = x.Name, Code = x.Code, DefaultCapacity = x.DefaultCapacity }).ToListAsync(ct);
    public async Task<IEnumerable<VenueTableDto>> GetTablesAsync(CancellationToken ct) => await db.VenueTables.Select(x => new VenueTableDto { TableId = x.TableId, ZoneId = x.ZoneId, TableTypeId = x.TableTypeId, TableCode = x.TableCode, TableName = x.TableName, Capacity = x.Capacity, OperationalStatus = x.OperationalStatus }).ToListAsync(ct);
    public async Task<IEnumerable<PricingPlanDto>> GetPricingPlansAsync(CancellationToken ct) => await db.PricingPlans.Select(x => new PricingPlanDto { PricingPlanId = x.PricingPlanId, Name = x.Name, IsDefault = x.IsDefault, IsActive = x.IsActive }).ToListAsync(ct);
    public async Task<IEnumerable<PricingPlanRuleDto>> GetPricingPlanRulesAsync(CancellationToken ct) => await db.PricingPlanRules.Select(x => new PricingPlanRuleDto { PricingPlanRuleId = x.PricingPlanRuleId, PricingPlanId = x.PricingPlanId, TableTypeId = x.TableTypeId, DayOfWeek = x.DayOfWeek, HourlyRate = x.HourlyRate }).ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<VenueLayoutResponse> GetLayoutAsync(CancellationToken ct)
    {
        // 1. Lấy tất cả các bàn đang active (IsActive=true), kèm thông tin Zone, Floor, TableType
        var tables = await db.VenueTables
            .Where(t => t.IsActive)
            .Join(db.Zones.Where(z => z.IsActive),
                t => t.ZoneId, z => z.ZoneId,
                (t, z) => new { Table = t, Zone = z })
            .Join(db.Floors.Where(f => f.IsActive),
                tz => tz.Zone.FloorId, f => f.FloorId,
                (tz, f) => new { tz.Table, tz.Zone, Floor = f })
            .Join(db.TableTypes,
                tzf => tzf.Table.TableTypeId, tt => tt.TableTypeId,
                (tzf, tt) => new
                {
                    tzf.Table,
                    tzf.Zone,
                    tzf.Floor,
                    TableTypeName = tt.Name
                })
            .ToListAsync(ct);

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

        // 3. Build Floor → Zone → Table hierarchy
        var floorGroups = tables
            .GroupBy(x => new { x.Floor.FloorId, x.Floor.Name, x.Floor.Description, x.Floor.DisplayOrder })
            .OrderBy(g => g.Key.DisplayOrder)
            .Select(floorGroup => new VenueFloorLayoutItem
            {
                FloorId = floorGroup.Key.FloorId,
                FloorName = floorGroup.Key.Name,
                Description = floorGroup.Key.Description,
                DisplayOrder = floorGroup.Key.DisplayOrder,
                Zones = floorGroup
                    .GroupBy(x => new { x.Zone.ZoneId, x.Zone.Name, x.Zone.Description, x.Zone.DisplayOrder })
                    .OrderBy(zg => zg.Key.DisplayOrder)
                    .Select(zoneGroup => new VenueZoneLayoutItem
                    {
                        ZoneId = zoneGroup.Key.ZoneId,
                        ZoneName = zoneGroup.Key.Name,
                        Description = zoneGroup.Key.Description,
                        DisplayOrder = zoneGroup.Key.DisplayOrder,
                        Tables = zoneGroup
                            .Select(x =>
                            {
                                // Ưu tiên lấy trạng thái từ session active, nếu không thì từ DB
                                var activeSessionId = activeSessionMap.TryGetValue(x.Table.TableId, out var sid) ? sid : (long?)null;
                                var operationalStatus = activeSessionId.HasValue ? 2 : x.Table.OperationalStatus; // 2=Occupied

                                return new VenueTableLayoutItem
                                {
                                    TableId = x.Table.TableId,
                                    TableCode = x.Table.TableCode,
                                    TableName = x.Table.TableName,
                                    TableTypeId = x.Table.TableTypeId,
                                    TableTypeName = x.TableTypeName,
                                    Capacity = x.Table.Capacity,
                                    OperationalStatus = operationalStatus,
                                    PositionX = x.Table.PositionX,
                                    PositionY = x.Table.PositionY,
                                    IsActive = x.Table.IsActive,
                                    ActiveSessionId = activeSessionId
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
            FetchedAtUtc = DateTime.UtcNow
        };
    }
}

