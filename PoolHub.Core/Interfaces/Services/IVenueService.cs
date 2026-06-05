using PoolHub.Core.DTOs.Venue;

namespace PoolHub.Core.Interfaces.Services;

public interface IVenueService
{
    Task<IEnumerable<FloorDto>> GetFloorsAsync(CancellationToken ct);
    Task<IEnumerable<ZoneDto>> GetZonesAsync(CancellationToken ct);
    Task<IEnumerable<TableTypeDto>> GetTableTypesAsync(CancellationToken ct);
    Task<IEnumerable<VenueTableDto>> GetTablesAsync(CancellationToken ct);
    Task<IEnumerable<PricingPlanDto>> GetPricingPlansAsync(CancellationToken ct);
    Task<IEnumerable<PricingPlanRuleDto>> GetPricingPlanRulesAsync(CancellationToken ct);
}
