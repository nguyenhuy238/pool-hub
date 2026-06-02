using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Order;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff + "," + RoleConstants.Cashier)]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpPost] public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] CreateOrderRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await orderService.CreateOrderAsync(User.GetUserId(), request, ct)));
    [HttpPost("{orderId:int}/items")] public async Task<ActionResult<ApiResponse<object>>> AddItem(int orderId, [FromBody] AddOrderItemRequest request, CancellationToken ct) { await orderService.AddOrderItemAsync(orderId, request, ct); return Ok(ApiResponse<object>.Ok(new { }, "Item added")); }
}
