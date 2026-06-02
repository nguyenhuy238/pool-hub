using Microsoft.EntityFrameworkCore;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Repositories;
using PoolHub.Infrastructure.Data;

namespace PoolHub.Infrastructure.Repositories;

public class ProductRepository(PoolHubDbContext db) : IProductRepository
{
    public IQueryable<ProductCategory> GetCategories() => db.ProductCategories.AsQueryable();
    public async Task AddCategoryAsync(ProductCategory category, CancellationToken ct) => await db.ProductCategories.AddAsync(category, ct);

    public IQueryable<Product> GetProducts() => db.Products.AsQueryable();
    public async Task<Product?> GetProductByIdAsync(int id, CancellationToken ct) => await db.Products.FindAsync([id], ct);
    
    public async Task<bool> ProductCodeExistsAsync(string code, CancellationToken ct) => await db.Products.AnyAsync(x => x.Code == code, ct);
    
    public async Task AddProductAsync(Product product, CancellationToken ct) => await db.Products.AddAsync(product, ct);

    public async Task AddInventoryTransactionAsync(InventoryTransaction transaction, CancellationToken ct) => await db.InventoryTransactions.AddAsync(transaction, ct);

    public async Task<int> SaveChangesAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);
}
