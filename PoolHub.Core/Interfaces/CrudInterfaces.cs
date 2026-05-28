using PoolHub.Core.DTOs.Booking;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Product;
using PoolHub.Core.DTOs.Venue;
using PoolHub.Shared;

namespace PoolHub.Core.Interfaces;

public interface ICrudService
{
    Task<PagedResult<FloorDto>> GetFloorsAsync(PaginationRequest request, CancellationToken ct);
    Task<FloorDto> GetFloorAsync(int id, CancellationToken ct);
    Task<FloorDto> CreateFloorAsync(FloorDto dto, CancellationToken ct);
    Task<FloorDto> UpdateFloorAsync(int id, FloorDto dto, CancellationToken ct);
    Task DeleteFloorAsync(int id, CancellationToken ct);

    Task<PagedResult<ZoneDto>> GetZonesAsync(PaginationRequest request, CancellationToken ct);
    Task<ZoneDto> GetZoneAsync(int id, CancellationToken ct);
    Task<ZoneDto> CreateZoneAsync(ZoneDto dto, CancellationToken ct);
    Task<ZoneDto> UpdateZoneAsync(int id, ZoneDto dto, CancellationToken ct);
    Task DeleteZoneAsync(int id, CancellationToken ct);

    Task<PagedResult<TableTypeDto>> GetTableTypesAsync(PaginationRequest request, CancellationToken ct);
    Task<TableTypeDto> GetTableTypeAsync(int id, CancellationToken ct);
    Task<TableTypeDto> CreateTableTypeAsync(TableTypeDto dto, CancellationToken ct);
    Task<TableTypeDto> UpdateTableTypeAsync(int id, TableTypeDto dto, CancellationToken ct);
    Task DeleteTableTypeAsync(int id, CancellationToken ct);

    Task<PagedResult<VenueTableDto>> GetVenueTablesAsync(PaginationRequest request, CancellationToken ct);
    Task<VenueTableDto> GetVenueTableAsync(int id, CancellationToken ct);
    Task<VenueTableDto> CreateVenueTableAsync(VenueTableDto dto, CancellationToken ct);
    Task<VenueTableDto> UpdateVenueTableAsync(int id, VenueTableDto dto, CancellationToken ct);
    Task DeleteVenueTableAsync(int id, CancellationToken ct);

    Task<PagedResult<PricingPlanDto>> GetPricingPlansAsync(PaginationRequest request, CancellationToken ct);
    Task<PagedResult<PricingPlanRuleDto>> GetPricingPlanRulesAsync(PaginationRequest request, CancellationToken ct);

    Task<PagedResult<ProductCategoryDto>> GetProductCategoriesAsync(PaginationRequest request, CancellationToken ct);
    Task<ProductCategoryDto> CreateProductCategoryAsync(ProductCategoryDto dto, CancellationToken ct);

    Task<PagedResult<BookingDto>> GetBookingsCrudAsync(PaginationRequest request, CancellationToken ct);
    Task<BookingDto> GetBookingAsync(int id, CancellationToken ct);
    Task DeleteBookingAsync(int id, CancellationToken ct);
}
