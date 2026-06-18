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
        return Ok(ApiResponse<object>.Ok(await notificationService.GetNotificationsAsync(User.GetUserId(), ct)));
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
        return NoContent();
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> ReadAll(CancellationToken ct)
    {
        await notificationService.MarkAllReadAsync(User.GetUserId(), User.IsInRole(RoleConstants.Admin), ct);
        return NoContent();
    }
}
