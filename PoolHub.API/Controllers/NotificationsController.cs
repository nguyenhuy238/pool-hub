using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get(CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await notificationService.GetNotificationsAsync(User.GetUserId(), ct)));
}
