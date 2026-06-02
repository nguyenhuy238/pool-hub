using PoolHub.Core.DTOs.Order;

namespace PoolHub.Core.Interfaces.Services;

public interface IOrderService
{
    Task<OrderDto> CreateOrderAsync(int userId, CreateOrderRequest request, CancellationToken ct);
    Task AddOrderItemAsync(int orderId, AddOrderItemRequest request, CancellationToken ct);
}
