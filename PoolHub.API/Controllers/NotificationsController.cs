using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpGet] public ActionResult<ApiResponse<object>> Get() => Ok(ApiResponse<object>.Ok(notificationService.GetDemoNotifications()));
}
