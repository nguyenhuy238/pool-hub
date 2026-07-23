using PoolHub.Core.DTOs.Booking;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Product;
using PoolHub.Core.DTOs.Venue;
using PoolHub.Shared;

namespace PoolHub.Core.Interfaces;

public interface ICrudService
{
    Task<PagedResult<FloorDto>> GetFloorsAsync(PaginationRequest request, CancellationToken ct);
    Task<FloorDto> GetFloorAsync(long id, CancellationToken ct);
    Task<FloorDto> CreateFloorAsync(FloorDto dto, CancellationToken ct);
    Task<FloorDto> UpdateFloorAsync(long id, FloorDto dto, CancellationToken ct);
    Task DeleteFloorAsync(long id, CancellationToken ct);

    Task<PagedResult<ZoneDto>> GetZonesAsync(PaginationRequest request, CancellationToken ct);
    Task<ZoneDto> GetZoneAsync(long id, CancellationToken ct);
    Task<ZoneDto> CreateZoneAsync(ZoneDto dto, CancellationToken ct);
    Task<ZoneDto> UpdateZoneAsync(long id, ZoneDto dto, CancellationToken ct);
    Task DeleteZoneAsync(long id, CancellationToken ct);

    Task<PagedResult<TableTypeDto>> GetTableTypesAsync(PaginationRequest request, CancellationToken ct);
    Task<TableTypeDto> GetTableTypeAsync(long id, CancellationToken ct);
    Task<TableTypeDto> CreateTableTypeAsync(TableTypeDto dto, CancellationToken ct);
    Task<TableTypeDto> UpdateTableTypeAsync(long id, TableTypeDto dto, CancellationToken ct);
    Task DeleteTableTypeAsync(long id, CancellationToken ct);

    Task<PagedResult<VenueTableDto>> GetVenueTablesAsync(PaginationRequest request, CancellationToken ct);
    Task<VenueTableDto> GetVenueTableAsync(long id, CancellationToken ct);
    Task<VenueTableDto> CreateVenueTableAsync(VenueTableDto dto, CancellationToken ct);
    Task<VenueTableDto> UpdateVenueTableAsync(long id, VenueTableDto dto, CancellationToken ct);
    Task DeleteVenueTableAsync(long id, CancellationToken ct);

    Task<PagedResult<PricingPlanDto>> GetPricingPlansAsync(PricingPlanPaginationRequest request, CancellationToken ct);
    Task<PricingPlanDto> GetPricingPlanAsync(long id, CancellationToken ct);
    Task<PricingPlanDto> CreatePricingPlanAsync(PricingPlanDto dto, CancellationToken ct);
    Task<PricingPlanDto> UpdatePricingPlanAsync(long id, PricingPlanDto dto, CancellationToken ct);
    Task DeletePricingPlanAsync(long id, CancellationToken ct);

    Task<PagedResult<PricingPlanRuleDto>> GetPricingPlanRulesAsync(PricingRulePaginationRequest request, CancellationToken ct);
    Task<PricingPlanRuleDto> GetPricingPlanRuleAsync(long id, CancellationToken ct);
    Task<PricingPlanRuleDto> CreatePricingPlanRuleAsync(long planId, PricingPlanRuleDto dto, CancellationToken ct);
    Task<PricingPlanRuleDto> UpdatePricingPlanRuleAsync(long planId, long ruleId, PricingPlanRuleDto dto, CancellationToken ct);
    Task DeletePricingPlanRuleAsync(long planId, long ruleId, CancellationToken ct);

    Task<PagedResult<PricingSpecialDateDto>> GetPricingSpecialDatesAsync(PaginationRequest request, CancellationToken ct);
    Task<PricingSpecialDateDto> GetPricingSpecialDateAsync(long id, CancellationToken ct);
    Task<PricingSpecialDateDto> CreatePricingSpecialDateAsync(PricingSpecialDateDto dto, CancellationToken ct);
    Task<PricingSpecialDateDto> UpdatePricingSpecialDateAsync(long id, PricingSpecialDateDto dto, CancellationToken ct);
    Task DeletePricingSpecialDateAsync(long id, CancellationToken ct);

    Task<PagedResult<ProductCategoryDto>> GetProductCategoriesAsync(PaginationRequest request, CancellationToken ct);
    Task<ProductCategoryDto> CreateProductCategoryAsync(ProductCategoryDto dto, CancellationToken ct);
    Task<ProductCategoryDto> UpdateProductCategoryAsync(int id, ProductCategoryDto dto, CancellationToken ct);
    Task DeleteProductCategoryAsync(int id, CancellationToken ct);

    Task<PagedResult<BookingDto>> GetBookingsCrudAsync(PaginationRequest request, CancellationToken ct);
    Task<BookingDto> GetBookingAsync(long id, CancellationToken ct);
    Task DeleteBookingAsync(long id, CancellationToken ct);

}
