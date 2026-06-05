using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Session;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/sessions")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff + "," + RoleConstants.Cashier)]
public class SessionsController(ISessionService sessionService) : ControllerBase
{
    [HttpPost("start")] public async Task<ActionResult<ApiResponse<object>>> Start([FromBody] StartSessionRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await sessionService.StartAsync(User.GetUserId(), request, ct)));
    [HttpPost("{sessionId:long}/end")] public async Task<ActionResult<ApiResponse<object>>> Close(long sessionId, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await sessionService.CloseAsync(sessionId, User.GetUserId(), ct)));
    [HttpPost("{sessionId:long}/switch")] public async Task<ActionResult<ApiResponse<object>>> Transfer(long sessionId, [FromBody] TransferTableRequest request, CancellationToken ct) { await sessionService.TransferTableAsync(sessionId, request.NewTableId, User.GetUserId(), ct); return Ok(ApiResponse<object>.Ok(new { }, "Switched")); }
}
