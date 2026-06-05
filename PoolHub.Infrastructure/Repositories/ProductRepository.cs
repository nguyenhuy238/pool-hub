using Microsoft.EntityFrameworkCore;
using PoolHub.Core.Entities;
using PoolHub.Infrastructure.Data;

namespace PoolHub.Infrastructure.Repositories;

public interface IProductRepository
{
    Task<List<Product>> GetLowStockProductsAsync(CancellationToken ct);
}

public class ProductRepository(PoolHubDbContext db) : IProductRepository
{
    public Task<List<Product>> GetLowStockProductsAsync(CancellationToken ct) =>
        db.Products.Where(x => x.LowStockThreshold.HasValue && x.StockQuantity <= x.LowStockThreshold.Value).ToListAsync(ct);
}
