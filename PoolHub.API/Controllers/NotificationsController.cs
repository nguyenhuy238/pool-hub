using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Shared;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    [HttpGet] public ActionResult<ApiResponse<object>> Get() => Ok(ApiResponse<object>.Ok(new[] { new { id = 1, title = "Demo notification" } }));
}
