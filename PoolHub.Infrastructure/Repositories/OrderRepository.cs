using Microsoft.EntityFrameworkCore;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Repositories;
using PoolHub.Infrastructure.Data;

namespace PoolHub.Infrastructure.Repositories;

public class OrderRepository(PoolHubDbContext db) : IOrderRepository
{
    public async Task<Order?> GetOrderByIdAsync(int id, CancellationToken ct) => await db.Orders.FindAsync([id], ct);
    
    public async Task AddOrderAsync(Order order, CancellationToken ct) => await db.Orders.AddAsync(order, ct);
    
    public async Task AddOrderItemAsync(OrderItem item, CancellationToken ct) => await db.OrderItems.AddAsync(item, ct);

    public async Task<decimal> GetTotalLineAmountBySessionAsync(int sessionId, CancellationToken ct)
    {
        return await (from o in db.Orders 
                      where o.SessionId == sessionId
                      join i in db.OrderItems on o.OrderId equals i.OrderId
                      select i.LineTotal).SumAsync(ct);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);
}
