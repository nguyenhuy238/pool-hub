using PoolHub.Core.Entities;

namespace PoolHub.Infrastructure.Repositories;

public interface IProductRepository
{
    Task<List<Product>> GetLowStockProductsAsync(CancellationToken ct);
}
