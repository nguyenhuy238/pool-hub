using PoolHub.Core.DTOs.Admin;
using PoolHub.Core.DTOs.Invoice;
using PoolHub.Shared;

namespace PoolHub.Core.Interfaces.Services;

public interface IAdminManagementService
{
    Task<PagedResult<DiscountDto>> GetDiscountsAsync(DiscountQueryRequest request, CancellationToken ct);
    Task<DiscountDto> CreateDiscountAsync(UpsertDiscountRequest request, long actorUserId, CancellationToken ct);
    Task<DiscountDto> UpdateDiscountAsync(long id, UpsertDiscountRequest request, long actorUserId, CancellationToken ct);
    Task SetDiscountStatusAsync(long id, bool isActive, long actorUserId, CancellationToken ct);
    Task<DiscountValidationDto> ValidateDiscountAsync(ValidateDiscountRequest request, CancellationToken ct);
    Task<PagedResult<InventoryTransactionDto>> GetInventoryAsync(InventoryQueryRequest request, CancellationToken ct);
    Task<InventoryTransactionDto> AdjustStockAsync(StockAdjustRequest request, long actorUserId, CancellationToken ct);
    Task<List<LowStockProductDto>> GetLowStockAsync(CancellationToken ct);
    Task<List<PaymentMethodDto>> GetAllPaymentMethodsAsync(CancellationToken ct);
    Task<PaymentMethodDto> CreatePaymentMethodAsync(UpsertPaymentMethodRequest request, long actorUserId, CancellationToken ct);
    Task<PaymentMethodDto> UpdatePaymentMethodAsync(long id, UpsertPaymentMethodRequest request, long actorUserId, CancellationToken ct);
    Task SetPaymentMethodStatusAsync(long id, bool isActive, long actorUserId, CancellationToken ct);
    Task<PagedResult<PaymentDto>> GetPaymentsAsync(PaymentQueryRequest request, CancellationToken ct);
    Task<List<RevenueReportDto>> GetRevenueReportAsync(ReportQueryRequest request, CancellationToken ct);
    Task<List<TableUsageReportDto>> GetTableUsageReportAsync(ReportQueryRequest request, CancellationToken ct);
    Task<List<ProductSalesReportDto>> GetProductReportAsync(ReportQueryRequest request, CancellationToken ct);
    Task<List<BookingReportDto>> GetBookingReportAsync(ReportQueryRequest request, CancellationToken ct);
}
