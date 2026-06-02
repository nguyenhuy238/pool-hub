using PoolHub.Core.Entities;

namespace PoolHub.Core.Interfaces.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetOrderByIdAsync(int id, CancellationToken ct);
    Task AddOrderAsync(Order order, CancellationToken ct);
    Task AddOrderItemAsync(OrderItem item, CancellationToken ct);
    Task<decimal> GetTotalLineAmountBySessionAsync(int sessionId, CancellationToken ct);
    
    Task<int> SaveChangesAsync(CancellationToken ct);
}
