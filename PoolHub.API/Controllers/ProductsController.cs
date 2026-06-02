using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Product;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController(IProductService productService) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] PaginationRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await productService.GetProductsAsync(request, ct)));
    [HttpGet("{id:int}")] public async Task<ActionResult<ApiResponse<object>>> GetById(int id, CancellationToken ct) { var p = await productService.GetProductsAsync(new PaginationRequest{PageNumber=1,PageSize=1000}, ct); var item = p.Items.FirstOrDefault(x => x.ProductId == id); if (item is null) return NotFound(ApiResponse<object>.Fail("Product not found.")); return Ok(ApiResponse<object>.Ok(item)); }
    [HttpGet("categories")] public async Task<ActionResult<ApiResponse<object>>> Categories([FromQuery] PaginationRequest r, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await productService.GetProductCategoriesAsync(r, ct)));
    [HttpPost("categories")] [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)] public async Task<ActionResult<ApiResponse<object>>> CreateCategory([FromBody] ProductCategoryDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await productService.CreateProductCategoryAsync(dto, ct)));
    [HttpPost] [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)] public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] CreateProductRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await productService.CreateProductAsync(request, ct)));
}
