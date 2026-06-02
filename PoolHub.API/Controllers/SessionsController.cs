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
    [HttpPost("{sessionId:int}/end")] public async Task<ActionResult<ApiResponse<object>>> Close(int sessionId, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await sessionService.CloseAsync(sessionId, ct)));
    [HttpPost("{sessionId:int}/switch")] public async Task<ActionResult<ApiResponse<object>>> Transfer(int sessionId, [FromBody] TransferTableRequest request, CancellationToken ct) { await sessionService.TransferTableAsync(sessionId, request.NewTableId, ct); return Ok(ApiResponse<object>.Ok(new { }, "Switched")); }
}
