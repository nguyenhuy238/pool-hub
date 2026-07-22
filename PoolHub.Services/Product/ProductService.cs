using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Product;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared;
using PoolHub.Shared.Exceptions;
using EntityProduct = PoolHub.Core.Entities.Product;

namespace PoolHub.Services.Product;

public class ProductService(PoolHubDbContext db) : IProductService
{
    public async Task<IEnumerable<ProductCategoryDto>> GetCategoriesAsync(CancellationToken ct) => await db.ProductCategories.Select(x => new ProductCategoryDto { ProductCategoryId = x.ProductCategoryId, Name = x.Name }).ToListAsync(ct);

    public async Task<PagedResult<ProductDto>> GetProductsAsync(PaginationRequest request, CancellationToken ct)
    {
        var query = db.Products.AsQueryable().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(request.Search)) query = query.Where(x => x.Name.Contains(request.Search) || x.Sku.Contains(request.Search));
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.ProductId).Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).Select(x => new ProductDto { ProductId = x.ProductId, ProductCategoryId = x.ProductCategoryId, Name = x.Name, Sku = x.Sku, UnitPrice = x.UnitPrice, StockQuantity = x.StockQuantity }).ToListAsync(ct);
        return new PagedResult<ProductDto> { Items = items, PageNumber = request.PageNumber, PageSize = request.PageSize, TotalCount = total };
    }

    public async Task<ProductDto> GetProductByIdAsync(long id, CancellationToken ct)
    {
        var product = await db.Products.FirstOrDefaultAsync(x => x.ProductId == id && x.IsActive, ct) ?? throw new NotFoundException("Product not found.");
        return new ProductDto { ProductId = product.ProductId, ProductCategoryId = product.ProductCategoryId, Name = product.Name, Sku = product.Sku, UnitPrice = product.UnitPrice, StockQuantity = product.StockQuantity };
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken ct)
    {
        if (await db.Products.AnyAsync(x => x.Sku == request.Sku, ct)) throw new ConflictException("Product sku already exists.");
        var product = new EntityProduct { ProductCategoryId = request.ProductCategoryId, Name = request.Name, Sku = request.Sku, UnitPrice = request.UnitPrice, StockQuantity = request.StockQuantity, IsActive = true };
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return new ProductDto { ProductId = product.ProductId, ProductCategoryId = product.ProductCategoryId, Name = product.Name, Sku = product.Sku, UnitPrice = product.UnitPrice, StockQuantity = product.StockQuantity };
    }

    public async Task<ProductDto> UpdateProductAsync(long id, UpdateProductRequest request, CancellationToken ct)
    {
        var product = await db.Products.FindAsync([id], ct)
            ?? throw new NotFoundException("Product not found.");

        if (await db.Products.AnyAsync(x => x.Sku == request.Sku && x.ProductId != id, ct))
            throw new ConflictException("Product sku already exists.");

        product.ProductCategoryId = request.ProductCategoryId;
        product.Name = request.Name;
        product.Sku = request.Sku;
        product.UnitPrice = request.UnitPrice;
        product.StockQuantity = request.StockQuantity;

        await db.SaveChangesAsync(ct);

        return new ProductDto
        {
            ProductId = product.ProductId,
            ProductCategoryId = product.ProductCategoryId,
            Name = product.Name,
            Sku = product.Sku,
            UnitPrice = product.UnitPrice,
            StockQuantity = product.StockQuantity
        };
    }

    public async Task DeleteProductAsync(long id, CancellationToken ct)
    {
        var product = await db.Products.FindAsync([id], ct)
            ?? throw new NotFoundException("Product not found.");

        product.IsActive = false;

        await db.SaveChangesAsync(ct);
    }
}
