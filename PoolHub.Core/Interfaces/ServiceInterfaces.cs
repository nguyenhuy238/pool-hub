using PoolHub.Core.DTOs.Auth;
using PoolHub.Core.DTOs.Booking;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Invoice;
using PoolHub.Core.DTOs.Order;
using PoolHub.Core.DTOs.Product;
using PoolHub.Core.DTOs.Session;
using PoolHub.Core.DTOs.Users;
using PoolHub.Core.DTOs.Venue;
using PoolHub.Shared;

namespace PoolHub.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, long? currentUserId, CancellationToken cancellationToken);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<UserDto> MeAsync(long userId, CancellationToken cancellationToken);
    Task ChangePasswordAsync(long userId, ChangePasswordRequest request, CancellationToken cancellationToken);
    Task<AuthResponse> RefreshTokenAsync(string token, CancellationToken cancellationToken);
    Task LogoutAsync(string token, CancellationToken cancellationToken);
}

public interface IUserService
{
    Task<PagedResult<UserDto>> GetUsersAsync(PaginationRequest request, CancellationToken cancellationToken);
    Task<UserDto> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);
    Task<UserDto> UpdateAsync(long id, UpdateUserRequest request, CancellationToken cancellationToken);
    Task UpdateRolesAsync(long id, UpdateUserRoleRequest request, CancellationToken cancellationToken);
    Task UpdateStatusAsync(long id, bool status, CancellationToken cancellationToken);
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

public interface IBookingService { Task<PagedResult<BookingDto>> GetBookingsAsync(PaginationRequest request, CancellationToken ct); Task<BookingDto> CreateAsync(CreateBookingRequest request, CancellationToken ct); }
public interface IProductService { Task<IEnumerable<ProductCategoryDto>> GetCategoriesAsync(CancellationToken ct); Task<PagedResult<ProductDto>> GetProductsAsync(PaginationRequest request, CancellationToken ct); Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken ct); }
public interface ISessionService { Task<SessionDto> StartAsync(long userId, StartSessionRequest request, CancellationToken ct); Task<SessionDto> CloseAsync(long sessionId, long? closedByUserId, CancellationToken ct); Task TransferTableAsync(long sessionId, long newTableId, long? assignedByUserId, CancellationToken ct); }
public interface IOrderService { Task<OrderDto> CreateOrderAsync(long userId, CreateOrderRequest request, CancellationToken ct); Task AddOrderItemAsync(long orderId, AddOrderItemRequest request, CancellationToken ct); }
public interface IInvoiceService { Task<InvoiceDto> GenerateFromSessionAsync(long sessionId, long? issuedByUserId, CancellationToken ct); Task CreatePaymentAsync(CreatePaymentRequest request, long? receivedByUserId, CancellationToken ct); }
public interface IAuditService { Task<PagedResult<object>> GetAuditLogsAsync(PaginationRequest request, CancellationToken ct); }
