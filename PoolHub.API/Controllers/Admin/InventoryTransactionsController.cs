using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Admin;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers;

[ApiController, Route("api/inventory-transactions")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager, Policy = PermissionConstants.InventoryManage)]
public class InventoryTransactionsController(IAdminManagementService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] InventoryQueryRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await service.GetInventoryAsync(request, ct)));
    [HttpPost("stock-adjust")] public async Task<ActionResult<ApiResponse<object>>> Adjust(StockAdjustRequest request, CancellationToken ct) => StatusCode(201, ApiResponse<object>.Ok(await service.AdjustStockAsync(request, User.GetUserId(), ct)));
    [HttpGet("low-stock")] public async Task<ActionResult<ApiResponse<object>>> LowStock(CancellationToken ct) => Ok(ApiResponse<object>.Ok(await service.GetLowStockAsync(ct)));
}
