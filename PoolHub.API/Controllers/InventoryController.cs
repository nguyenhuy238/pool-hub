using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.Interfaces;
using PoolHub.Shared;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/inventory-transactions")]
[Authorize]
public class InventoryController(IProductService productService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] int? productId, [FromQuery] PaginationRequest request, CancellationToken ct)
        => Ok(ApiResponse<object>.Ok(await productService.GetInventoryTransactionsAsync(productId, request, ct)));
}
