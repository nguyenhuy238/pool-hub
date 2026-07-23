using PoolHub.Core.Entities;

namespace PoolHub.Infrastructure.Repositories;

public interface IOrderRepository
{
    Task<List<Order>> GetBySessionAsync(long sessionId, CancellationToken ct);
}
