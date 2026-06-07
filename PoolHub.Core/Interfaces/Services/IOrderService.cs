using PoolHub.Core.DTOs.Order;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PoolHub.Core.Interfaces.Services;

public interface IOrderService
{
    Task<OrderDetailDto> GetOrderByIdAsync(long orderId, CancellationToken ct);
    Task<List<OrderDetailDto>> GetOrdersBySessionIdAsync(long sessionId, CancellationToken ct);
    Task<OrderDto> CreateOrderAsync(long userId, CreateOrderRequest request, CancellationToken ct);
    Task AddOrderItemAsync(long orderId, AddOrderItemRequest request, CancellationToken ct);
    Task UpdateOrderItemAsync(long orderId, long itemId, int quantity, CancellationToken ct);
    Task DeleteOrderItemAsync(long orderId, long itemId, CancellationToken ct);
    Task CancelOrderAsync(long orderId, CancellationToken ct);
}
