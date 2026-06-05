using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Product;
using PoolHub.Core.Entities;
using PoolHub.Shared;

namespace PoolHub.Services.Common;

public partial class CrudService
{
    public async Task<PagedResult<ProductCategoryDto>> GetProductCategoriesAsync(PaginationRequest r, CancellationToken ct)
    {
        var q = db.ProductCategories.AsQueryable();
        var t = await q.CountAsync(ct);
        var i = await q.Skip((r.PageNumber - 1) * r.PageSize).Take(r.PageSize).Select(x => new ProductCategoryDto { ProductCategoryId = x.ProductCategoryId, Name = x.Name }).ToListAsync(ct);
        return Page(i, r.PageNumber, r.PageSize, t);
    }

    public async Task<ProductCategoryDto> CreateProductCategoryAsync(ProductCategoryDto d, CancellationToken ct)
    {
        var x = new ProductCategory { Name = d.Name, IsActive = true };
        db.ProductCategories.Add(x);
        await db.SaveChangesAsync(ct);
        return new ProductCategoryDto { ProductCategoryId = x.ProductCategoryId, Name = x.Name };
    }
}
