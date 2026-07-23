using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Order;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Extensions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff)]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<OrderDetailDto>>> GetById(long id, CancellationToken ct) => 
        Ok(ApiResponse<OrderDetailDto>.Ok(await orderService.GetOrderByIdAsync(id, ct)));

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<OrderDetailDto>>>> GetBySession([FromQuery] long sessionId, CancellationToken ct) => 
        Ok(ApiResponse<List<OrderDetailDto>>.Ok(await orderService.GetOrdersBySessionIdAsync(sessionId, ct)));

    [HttpPost] 
    public async Task<ActionResult<ApiResponse<OrderDto>>> Create([FromBody] CreateOrderRequest request, CancellationToken ct) => 
        Ok(ApiResponse<OrderDto>.Ok(await orderService.CreateOrderAsync(User.GetUserId(), request, ct)));

    [HttpPost("{orderId:long}/items")] 
    public async Task<ActionResult<ApiResponse<object>>> AddItem(long orderId, [FromBody] AddOrderItemRequest request, CancellationToken ct) 
    { 
        await orderService.AddOrderItemAsync(orderId, request, ct); 
        return Ok(ApiResponse<object>.Ok(new { }, "Item added")); 
    }

    [HttpPut("{orderId:long}/items/{itemId:long}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateItem(long orderId, long itemId, [FromBody] UpdateOrderItemRequest request, CancellationToken ct)
    {
        await orderService.UpdateOrderItemAsync(orderId, itemId, request.Quantity, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Item updated"));
    }

    [HttpDelete("{orderId:long}/items/{itemId:long}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteItem(long orderId, long itemId, CancellationToken ct)
    {
        await orderService.DeleteOrderItemAsync(orderId, itemId, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Item deleted"));
    }

    [HttpPut("{orderId:long}/cancel")]
    public async Task<ActionResult<ApiResponse<object>>> Cancel(long orderId, CancellationToken ct)
    {
        await orderService.CancelOrderAsync(orderId, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Order cancelled"));
    }
}
