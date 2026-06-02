using PoolHub.Core.DTOs.Auth;
using PoolHub.Core.DTOs.AuditLog;
using PoolHub.Core.DTOs.Booking;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Invoice;
using PoolHub.Core.DTOs.Notification;
using PoolHub.Core.DTOs.Order;
using PoolHub.Core.DTOs.Product;
using PoolHub.Core.DTOs.Session;
using PoolHub.Core.DTOs.Users;
using PoolHub.Core.DTOs.Venue;
using PoolHub.Shared;

namespace PoolHub.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, int? currentUserId, CancellationToken cancellationToken);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<UserDto> MeAsync(int userId, CancellationToken cancellationToken);
    Task ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken);
    Task<AuthResponse> RefreshTokenAsync(string token, CancellationToken cancellationToken);
    Task LogoutAsync(string token, CancellationToken cancellationToken);
}

public interface IUserService
{
    Task<PagedResult<UserDto>> GetUsersAsync(PaginationRequest request, CancellationToken cancellationToken);
    Task<UserDto> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);
    Task<UserDto> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken);
    Task UpdateRolesAsync(int id, UpdateUserRoleRequest request, CancellationToken cancellationToken);
    Task UpdateStatusAsync(int id, bool status, CancellationToken cancellationToken);
    Task<List<string>> GetRolesAsync(CancellationToken cancellationToken);
}

public interface IVenueService
{
    Task<IEnumerable<FloorDto>> GetFloorsAsync(CancellationToken ct);
    Task<IEnumerable<ZoneDto>> GetZonesAsync(CancellationToken ct);
    Task<IEnumerable<TableTypeDto>> GetTableTypesAsync(CancellationToken ct);
    Task<IEnumerable<VenueTableDto>> GetTablesAsync(CancellationToken ct);
    Task<IEnumerable<PricingPlanDto>> GetPricingPlansAsync(CancellationToken ct);
    Task<IEnumerable<PricingPlanRuleDto>> GetPricingPlanRulesAsync(CancellationToken ct);
}

public interface IBookingService { Task<PagedResult<BookingDto>> GetBookingsAsync(PaginationRequest request, CancellationToken ct); Task<BookingDto> CreateAsync(CreateBookingRequest request, CancellationToken ct); Task<BookingDto> ConfirmAsync(int bookingId, int confirmedByUserId, CancellationToken ct); }

public interface IProductService
{
    Task<IEnumerable<ProductCategoryDto>> GetCategoriesAsync(CancellationToken ct);
    Task<PagedResult<ProductDto>> GetProductsAsync(PaginationRequest request, CancellationToken ct);
    Task<ProductDto> GetProductByIdAsync(int id, CancellationToken ct);
    Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken ct);
    Task<ProductDto> UpdateProductAsync(int id, UpdateProductRequest request, CancellationToken ct);
    Task DeleteProductAsync(int id, CancellationToken ct);
    Task<ProductDto> StockAdjustAsync(int id, StockAdjustRequest request, int userId, CancellationToken ct);
    Task<ProductCategoryDto> UpdateCategoryAsync(int id, ProductCategoryDto dto, CancellationToken ct);
    Task DeleteCategoryAsync(int id, CancellationToken ct);
    Task<PagedResult<InventoryTransactionDto>> GetInventoryTransactionsAsync(int? productId, PaginationRequest request, CancellationToken ct);
}

public interface ISessionService { Task<SessionDto> StartAsync(int userId, StartSessionRequest request, CancellationToken ct); Task<SessionDto> CloseAsync(int sessionId, CancellationToken ct); Task TransferTableAsync(int sessionId, int newTableId, CancellationToken ct); }
public interface IOrderService { Task<OrderDto> CreateOrderAsync(int userId, CreateOrderRequest request, CancellationToken ct); Task AddOrderItemAsync(int orderId, AddOrderItemRequest request, CancellationToken ct); }
public interface IInvoiceService { Task<InvoiceDto> GenerateFromSessionAsync(int sessionId, CancellationToken ct); Task CreatePaymentAsync(CreatePaymentRequest request, CancellationToken ct); }

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetUserNotificationsAsync(int userId, PaginationRequest request, CancellationToken ct);
    Task MarkAsReadAsync(int notificationId, int userId, CancellationToken ct);
    Task MarkAllAsReadAsync(int userId, CancellationToken ct);
    Task CreateNotificationAsync(int userId, string title, string content, CancellationToken ct);
}

public interface IAuditService
{
    Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(AuditLogFilterRequest request, CancellationToken ct);
    Task LogAsync(int? userId, string action, string entityName, string entityId, string? metadata, CancellationToken ct);
}
