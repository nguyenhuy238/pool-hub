using Microsoft.EntityFrameworkCore;
using PoolHub.Core.Entities;
using PoolHub.Infrastructure.Data;

namespace PoolHub.Infrastructure.Repositories;

public class OrderRepository(PoolHubDbContext db) : IOrderRepository
{
    public Task<List<Order>> GetBySessionAsync(long sessionId, CancellationToken ct) =>
        db.Orders.Where(x => x.SessionId == sessionId).ToListAsync(ct);
}
