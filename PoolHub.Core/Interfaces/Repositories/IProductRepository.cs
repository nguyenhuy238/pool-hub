using PoolHub.Core.Entities;

namespace PoolHub.Core.Interfaces.Repositories;

public interface IProductRepository
{
    IQueryable<ProductCategory> GetCategories();
    Task AddCategoryAsync(ProductCategory category, CancellationToken ct);

    IQueryable<Product> GetProducts();
    Task<Product?> GetProductByIdAsync(int id, CancellationToken ct);
    Task<bool> ProductCodeExistsAsync(string code, CancellationToken ct);
    Task AddProductAsync(Product product, CancellationToken ct);

    Task AddInventoryTransactionAsync(InventoryTransaction transaction, CancellationToken ct);

    Task<int> SaveChangesAsync(CancellationToken ct);
}
