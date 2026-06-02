using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.Interfaces;
using PoolHub.Shared;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] PaginationRequest request, CancellationToken ct)
        => Ok(ApiResponse<object>.Ok(await notificationService.GetUserNotificationsAsync(User.GetUserId(), request, ct)));

    [HttpPut("{id:int}/read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAsRead(int id, CancellationToken ct)
    {
        await notificationService.MarkAsReadAsync(id, User.GetUserId(), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Marked as read"));
    }

    [HttpPut("read-all")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAllAsRead(CancellationToken ct)
    {
        await notificationService.MarkAllAsReadAsync(User.GetUserId(), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "All notifications marked as read"));
    }
}
