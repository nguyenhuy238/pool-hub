using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Product;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Repositories;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Exceptions;

namespace PoolHub.Services.Services.Product;

public class ProductService(IProductRepository repo) : IProductService
{
    private static PagedResult<T> Page<T>(IReadOnlyCollection<T> items, int page, int size, int total) => new() { Items = items, PageNumber = page, PageSize = size, TotalCount = total };

    public async Task<IEnumerable<ProductCategoryDto>> GetCategoriesAsync(CancellationToken ct) => await repo.GetCategories().Select(x => new ProductCategoryDto { CategoryId = x.CategoryId, Name = x.Name, Code = x.Code }).ToListAsync(ct);

    public async Task<PagedResult<ProductCategoryDto>> GetProductCategoriesAsync(PaginationRequest request, CancellationToken ct)
    {
        var q = repo.GetCategories();
        var t = await q.CountAsync(ct);
        var i = await q.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).Select(x => new ProductCategoryDto { CategoryId = x.CategoryId, Name = x.Name, Code = x.Code }).ToListAsync(ct);
        return Page(i, request.PageNumber, request.PageSize, t);
    }

    public async Task<ProductCategoryDto> CreateProductCategoryAsync(ProductCategoryDto dto, CancellationToken ct)
    {
        var x = new ProductCategory { Name = dto.Name, Code = dto.Code, IsActive = true };
        await repo.AddCategoryAsync(x, ct);
        await repo.SaveChangesAsync(ct);
        return new ProductCategoryDto { CategoryId = x.CategoryId, Name = x.Name, Code = x.Code };
    }

    public async Task<PagedResult<ProductDto>> GetProductsAsync(PaginationRequest request, CancellationToken ct)
    {
        var query = repo.GetProducts();
        if (!string.IsNullOrWhiteSpace(request.Search)) query = query.Where(x => x.Name.Contains(request.Search) || x.Code.Contains(request.Search));
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.ProductId).Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).Select(x => new ProductDto { ProductId = x.ProductId, CategoryId = x.CategoryId, Name = x.Name, Code = x.Code, UnitPrice = x.UnitPrice, StockQuantity = x.StockQuantity }).ToListAsync(ct);
        return Page(items, request.PageNumber, request.PageSize, total);
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken ct)
    {
        if (await repo.ProductCodeExistsAsync(request.Code, ct)) throw new ConflictException("Product code already exists.");
        var product = new PoolHub.Core.Entities.Product { CategoryId = request.CategoryId, Name = request.Name, Code = request.Code, UnitPrice = request.UnitPrice, StockQuantity = request.StockQuantity, IsActive = true };
        await repo.AddProductAsync(product, ct);
        await repo.SaveChangesAsync(ct);
        return new ProductDto { ProductId = product.ProductId, CategoryId = product.CategoryId, Name = product.Name, Code = product.Code, UnitPrice = product.UnitPrice, StockQuantity = product.StockQuantity };
    }
}
