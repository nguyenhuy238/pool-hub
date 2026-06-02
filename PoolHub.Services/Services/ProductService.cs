using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Product;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared;
using PoolHub.Shared.Exceptions;

namespace PoolHub.Services.Services;

public class ProductService(PoolHubDbContext db, IAuditService auditService) : IProductService
{
    public async Task<IEnumerable<ProductCategoryDto>> GetCategoriesAsync(CancellationToken ct)
        => await db.ProductCategories
            .Where(x => x.IsActive)
            .Select(x => new ProductCategoryDto { CategoryId = x.CategoryId, Name = x.Name, Code = x.Code, Description = x.Description, IsActive = x.IsActive })
            .ToListAsync(ct);

    public async Task<PagedResult<ProductDto>> GetProductsAsync(PaginationRequest request, CancellationToken ct)
    {
        var query = db.Products.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(x => x.Name.Contains(request.Search) || x.Code.Contains(request.Search));
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(x => x.ProductId)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new ProductDto
            {
                ProductId = x.ProductId, CategoryId = x.CategoryId, Name = x.Name,
                Code = x.Code, UnitPrice = x.UnitPrice, StockQuantity = x.StockQuantity, IsActive = x.IsActive
            })
            .ToListAsync(ct);
        return new PagedResult<ProductDto> { Items = items, PageNumber = request.PageNumber, PageSize = request.PageSize, TotalCount = total };
    }

    public async Task<ProductDto> GetProductByIdAsync(int id, CancellationToken ct)
    {
        var x = await db.Products.FindAsync([id], ct) ?? throw new NotFoundException("Product not found.");
        return new ProductDto
        {
            ProductId = x.ProductId, CategoryId = x.CategoryId, Name = x.Name,
            Code = x.Code, UnitPrice = x.UnitPrice, StockQuantity = x.StockQuantity, IsActive = x.IsActive
        };
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken ct)
    {
        if (await db.Products.AnyAsync(x => x.Code == request.Code, ct))
            throw new ConflictException("Product code already exists.");
        var product = new Product
        {
            CategoryId = request.CategoryId, Name = request.Name, Code = request.Code,
            UnitPrice = request.UnitPrice, StockQuantity = request.StockQuantity, IsActive = true
        };
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        await auditService.LogAsync(null, "CREATE", "Product", product.ProductId.ToString(), $"Created product {product.Code}", ct);
        return await GetProductByIdAsync(product.ProductId, ct);
    }

    public async Task<ProductDto> UpdateProductAsync(int id, UpdateProductRequest request, CancellationToken ct)
    {
        var product = await db.Products.FindAsync([id], ct) ?? throw new NotFoundException("Product not found.");
        product.CategoryId = request.CategoryId;
        product.Name = request.Name;
        product.UnitPrice = request.UnitPrice;
        product.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await auditService.LogAsync(null, "UPDATE", "Product", id.ToString(), $"Updated product {product.Code}", ct);
        return await GetProductByIdAsync(id, ct);
    }

    public async Task DeleteProductAsync(int id, CancellationToken ct)
    {
        var product = await db.Products.FindAsync([id], ct) ?? throw new NotFoundException("Product not found.");
        product.IsActive = false;
        product.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await auditService.LogAsync(null, "DELETE", "Product", id.ToString(), $"Soft-deleted product {product.Code}", ct);
    }

    public async Task<ProductDto> StockAdjustAsync(int id, StockAdjustRequest request, int userId, CancellationToken ct)
    {
        var product = await db.Products.FindAsync([id], ct) ?? throw new NotFoundException("Product not found.");

        var newStock = product.StockQuantity + request.QuantityChange;
        if (newStock < 0)
            throw new BusinessRuleException($"Stock cannot be negative. Current: {product.StockQuantity}, change: {request.QuantityChange}.");

        product.StockQuantity = newStock;
        product.UpdatedAtUtc = DateTime.UtcNow;

        db.InventoryTransactions.Add(new InventoryTransaction
        {
            ProductId = id,
            QuantityChange = request.QuantityChange,
            StockAfter = newStock,
            Reason = request.Reason,
            CreatedByUserId = userId
        });

        await db.SaveChangesAsync(ct);
        await auditService.LogAsync(userId, "STOCK_ADJUST", "Product", id.ToString(),
            $"Qty change: {request.QuantityChange}, reason: {request.Reason}, stock after: {newStock}", ct);
        return await GetProductByIdAsync(id, ct);
    }

    public async Task<ProductCategoryDto> UpdateCategoryAsync(int id, ProductCategoryDto dto, CancellationToken ct)
    {
        var category = await db.ProductCategories.FindAsync([id], ct) ?? throw new NotFoundException("Category not found.");
        category.Name = dto.Name;
        category.Code = dto.Code;
        category.Description = dto.Description;
        category.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new ProductCategoryDto { CategoryId = category.CategoryId, Name = category.Name, Code = category.Code, Description = category.Description, IsActive = category.IsActive };
    }

    public async Task DeleteCategoryAsync(int id, CancellationToken ct)
    {
        var category = await db.ProductCategories.FindAsync([id], ct) ?? throw new NotFoundException("Category not found.");
        category.IsActive = false;
        category.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<InventoryTransactionDto>> GetInventoryTransactionsAsync(int? productId, PaginationRequest request, CancellationToken ct)
    {
        var query = db.InventoryTransactions.AsQueryable();
        if (productId.HasValue)
            query = query.Where(x => x.ProductId == productId.Value);
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.InventoryTransactionId)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new InventoryTransactionDto
            {
                InventoryTransactionId = x.InventoryTransactionId,
                ProductId = x.ProductId,
                QuantityChange = x.QuantityChange,
                StockAfter = x.StockAfter,
                Reason = x.Reason,
                CreatedByUserId = x.CreatedByUserId,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(ct);
        return new PagedResult<InventoryTransactionDto> { Items = items, PageNumber = request.PageNumber, PageSize = request.PageSize, TotalCount = total };
    }
}
