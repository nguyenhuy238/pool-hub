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
}
