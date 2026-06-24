using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Product;
using PoolHub.Core.Interfaces;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared;
using PoolHub.Shared.Exceptions;

namespace PoolHub.Services.Common;

public partial class CrudService : ICrudService
{
    protected readonly PoolHubDbContext db;

    public CrudService(PoolHubDbContext db)
    {
        this.db = db;
    }

    private static PagedResult<T> Page<T>(IReadOnlyCollection<T> items, int page, int size, int total) => new() { Items = items, PageNumber = page, PageSize = size, TotalCount = total };

    public async Task<ProductCategoryDto> UpdateProductCategoryAsync(
        int id,
        ProductCategoryDto dto,
        CancellationToken ct)
    {
        var category = await db.ProductCategories
            .FirstOrDefaultAsync(x => x.ProductCategoryId == id, ct);

        if (category == null)
            throw new NotFoundException("Product category not found.");

        category.Name = dto.Name;

        await db.SaveChangesAsync(ct);

        return new ProductCategoryDto
        {
            ProductCategoryId = category.ProductCategoryId,
            Name = category.Name
        };
    }

    public async Task DeleteProductCategoryAsync(int id, CancellationToken ct)
    {
        var category = await db.ProductCategories.FirstOrDefaultAsync(x => x.ProductCategoryId == id, ct)
            ?? throw new NotFoundException("Product category not found.");
        if (await db.Products.AnyAsync(x => x.ProductCategoryId == id && x.IsActive, ct))
            throw new ConflictException("Product category still has active products.");
        category.IsActive = false;
        category.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

}
