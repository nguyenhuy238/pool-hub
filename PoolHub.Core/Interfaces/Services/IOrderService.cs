using PoolHub.Core.DTOs.Order;

namespace PoolHub.Core.Interfaces.Services;

public interface IOrderService
{
    Task<OrderDto> CreateOrderAsync(long userId, CreateOrderRequest request, CancellationToken ct);
    Task AddOrderItemAsync(long orderId, AddOrderItemRequest request, CancellationToken ct);
}
