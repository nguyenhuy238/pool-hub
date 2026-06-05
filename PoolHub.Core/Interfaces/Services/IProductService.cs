using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Product;
using PoolHub.Shared;

namespace PoolHub.Core.Interfaces.Services;

public interface IProductService
{
    Task<IEnumerable<ProductCategoryDto>> GetCategoriesAsync(CancellationToken ct);
    Task<PagedResult<ProductDto>> GetProductsAsync(PaginationRequest request, CancellationToken ct);
    Task<ProductDto> GetProductByIdAsync(long id, CancellationToken ct);
    Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken ct);
}
