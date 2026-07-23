using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Product;
using PoolHub.Core.Interfaces;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController(IProductService productService, ICrudService crud) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] PaginationRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await productService.GetProductsAsync(request, ct)));
    [HttpGet("{id:int}")] public async Task<ActionResult<ApiResponse<object>>> GetById(int id, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await productService.GetProductByIdAsync(id, ct)));
    [HttpGet("categories")] public async Task<ActionResult<ApiResponse<object>>> Categories([FromQuery] PaginationRequest r, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await crud.GetProductCategoriesAsync(r, ct)));
    [HttpPost("categories")] [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager, Policy = PermissionConstants.ProductsManage)] public async Task<ActionResult<ApiResponse<object>>> CreateCategory([FromBody] ProductCategoryDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await crud.CreateProductCategoryAsync(dto, ct)));
    [HttpPut("categories/{id:int}")] [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager, Policy = PermissionConstants.ProductsManage)] public async Task<ActionResult<ApiResponse<object>>> UpdateCategory(int id, [FromBody] ProductCategoryDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await crud.UpdateProductCategoryAsync(id, dto, ct)));
    [HttpDelete("categories/{id:int}")] [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager, Policy = PermissionConstants.ProductsManage)] public async Task<ActionResult<ApiResponse<object>>> DeleteCategory(int id, CancellationToken ct) { await crud.DeleteProductCategoryAsync(id, ct); return Ok(ApiResponse<object>.Ok(new { }, "Product category deleted.")); }
    [HttpPost] [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager, Policy = PermissionConstants.ProductsManage)] public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] CreateProductRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await productService.CreateProductAsync(request, ct)));

    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager, Policy = PermissionConstants.ProductsManage)]
    public async Task<ActionResult<ApiResponse<object>>> Update(
    int id,
    [FromBody] UpdateProductRequest request,
    CancellationToken ct)
    => Ok(ApiResponse<object>.Ok(await productService.UpdateProductAsync(id, request, ct)));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager, Policy = PermissionConstants.ProductsManage)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
    int id,
    CancellationToken ct)
    {
        await productService.DeleteProductAsync(id, ct);

        return Ok(ApiResponse<object>.Ok("Product deleted successfully."));
    }
}
