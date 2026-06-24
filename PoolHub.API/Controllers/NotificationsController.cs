using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Extensions;
using PoolHub.Shared.Constants;
using PoolHub.Core.DTOs.Notification;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] NotificationQueryRequest request, CancellationToken ct)
    {
        if (User.IsInRole(RoleConstants.Admin))
            return Ok(ApiResponse<object>.Ok(await notificationService.GetNotificationsAsync(request, ct)));
        return Ok(ApiResponse<object>.Ok(await notificationService.GetNotificationsAsync(User.GetUserId(), request, ct)));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> GetById(long id, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await notificationService.GetNotificationAsync(id, User.GetUserId(), User.IsInRole(RoleConstants.Admin), ct)));

    [HttpPost, Authorize(Roles = RoleConstants.Admin)]
    public async Task<ActionResult<ApiResponse<object>>> Create(CreateNotificationRequest request, CancellationToken ct) =>
        StatusCode(201, ApiResponse<object>.Ok(await notificationService.CreateAsync(request, ct)));

    [HttpPatch("{id:long}/read")]
    public async Task<IActionResult> Read(long id, CancellationToken ct)
    {
        await notificationService.MarkReadAsync(id, User.GetUserId(), User.IsInRole(RoleConstants.Admin), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Notification marked as read."));
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> ReadAll(CancellationToken ct)
    {
        await notificationService.MarkAllReadAsync(User.GetUserId(), User.IsInRole(RoleConstants.Admin), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "All notifications marked as read."));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<ApiResponse<object>>> GetUnreadCount(CancellationToken ct)
    {
        var count = await notificationService.GetUnreadCountAsync(User.GetUserId(), ct);
        return Ok(ApiResponse<object>.Ok(new { count }));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await notificationService.DeleteAsync(id, User.GetUserId(), User.IsInRole(RoleConstants.Admin), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Notification deleted."));
    }
}
