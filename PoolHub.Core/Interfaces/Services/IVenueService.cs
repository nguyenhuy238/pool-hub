using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Venue;
using PoolHub.Shared;

namespace PoolHub.Core.Interfaces.Services;

public interface IVenueService
{
    // Floor
    Task<PagedResult<FloorDto>> GetFloorsAsync(PaginationRequest request, CancellationToken ct);
    Task<FloorDto> GetFloorAsync(int id, CancellationToken ct);
    Task<FloorDto> CreateFloorAsync(FloorDto dto, CancellationToken ct);
    Task<FloorDto> UpdateFloorAsync(int id, FloorDto dto, CancellationToken ct);
    Task DeleteFloorAsync(int id, CancellationToken ct);
    Task<IEnumerable<FloorDto>> GetAllFloorsAsync(CancellationToken ct); // For non-paged endpoints

    // Zone
    Task<PagedResult<ZoneDto>> GetZonesAsync(PaginationRequest request, CancellationToken ct);
    Task<ZoneDto> GetZoneAsync(int id, CancellationToken ct);
    Task<ZoneDto> CreateZoneAsync(ZoneDto dto, CancellationToken ct);
    Task<ZoneDto> UpdateZoneAsync(int id, ZoneDto dto, CancellationToken ct);
    Task DeleteZoneAsync(int id, CancellationToken ct);
    Task<IEnumerable<ZoneDto>> GetAllZonesAsync(CancellationToken ct);

    // TableType
    Task<PagedResult<TableTypeDto>> GetTableTypesAsync(PaginationRequest request, CancellationToken ct);
    Task<TableTypeDto> GetTableTypeAsync(int id, CancellationToken ct);
    Task<TableTypeDto> CreateTableTypeAsync(TableTypeDto dto, CancellationToken ct);
    Task<TableTypeDto> UpdateTableTypeAsync(int id, TableTypeDto dto, CancellationToken ct);
    Task DeleteTableTypeAsync(int id, CancellationToken ct);
    Task<IEnumerable<TableTypeDto>> GetAllTableTypesAsync(CancellationToken ct);

    // VenueTable
    Task<PagedResult<VenueTableDto>> GetVenueTablesAsync(PaginationRequest request, CancellationToken ct);
    Task<VenueTableDto> GetVenueTableAsync(int id, CancellationToken ct);
    Task<VenueTableDto> CreateVenueTableAsync(VenueTableDto dto, CancellationToken ct);
    Task<VenueTableDto> UpdateVenueTableAsync(int id, VenueTableDto dto, CancellationToken ct);
    Task DeleteVenueTableAsync(int id, CancellationToken ct);
    Task<IEnumerable<VenueTableDto>> GetAllTablesAsync(CancellationToken ct);

    // PricingPlan
    Task<PagedResult<PricingPlanDto>> GetPricingPlansAsync(PaginationRequest request, CancellationToken ct);
    Task<PricingPlanDto> GetPricingPlanAsync(int id, CancellationToken ct);
    Task<PricingPlanDto> CreatePricingPlanAsync(PricingPlanDto dto, CancellationToken ct);
    Task<PricingPlanDto> UpdatePricingPlanAsync(int id, PricingPlanDto dto, CancellationToken ct);
    Task DeletePricingPlanAsync(int id, CancellationToken ct);
    Task<IEnumerable<PricingPlanDto>> GetAllPricingPlansAsync(CancellationToken ct);

    // PricingPlanRule
    Task<PagedResult<PricingPlanRuleDto>> GetPricingPlanRulesAsync(PaginationRequest request, CancellationToken ct);
    Task<PricingPlanRuleDto> GetPricingPlanRuleAsync(int id, CancellationToken ct);
    Task<PricingPlanRuleDto> CreatePricingPlanRuleAsync(PricingPlanRuleDto dto, CancellationToken ct);
    Task<PricingPlanRuleDto> UpdatePricingPlanRuleAsync(int id, PricingPlanRuleDto dto, CancellationToken ct);
    Task DeletePricingPlanRuleAsync(int id, CancellationToken ct);
    Task<IEnumerable<PricingPlanRuleDto>> GetAllPricingPlanRulesAsync(CancellationToken ct);
}
