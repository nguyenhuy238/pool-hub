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
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<SessionDto>>>> GetSessions([FromQuery] SessionQueryRequest request, CancellationToken ct) => 
        Ok(ApiResponse<PagedResult<SessionDto>>.Ok(await sessionService.GetSessionsAsync(request, ct)));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<SessionDetailDto>>> GetSessionById(long id, CancellationToken ct) => 
        Ok(ApiResponse<SessionDetailDto>.Ok(await sessionService.GetSessionByIdAsync(id, ct)));

    [HttpPost("start")] 
    public async Task<ActionResult<ApiResponse<SessionDto>>> Start([FromBody] StartSessionRequest request, CancellationToken ct) => 
        Ok(ApiResponse<SessionDto>.Ok(await sessionService.StartAsync(User.GetUserId(), request, ct)));

    [HttpPost("{sessionId:long}/end")] 
    public async Task<ActionResult<ApiResponse<SessionDto>>> Close(long sessionId, CancellationToken ct) => 
        Ok(ApiResponse<SessionDto>.Ok(await sessionService.CloseAsync(sessionId, User.GetUserId(), ct)));

    [HttpPost("{id:long}/close")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff)]
    public async Task<ActionResult<ApiResponse<CloseSessionResponse>>> CloseWithSummary(
        long id,
        [FromBody] CloseSessionRequest request,
        CancellationToken ct) =>
        Ok(ApiResponse<CloseSessionResponse>.Ok(
            await sessionService.CloseWithSummaryAsync(id, User.GetUserId(), request, ct),
            "Session closed"));

    [HttpPost("{sessionId:long}/switch")] 
    public async Task<ActionResult<ApiResponse<object>>> Transfer(long sessionId, [FromBody] TransferTableRequest request, CancellationToken ct) 
    { 
        await sessionService.TransferTableAsync(sessionId, request.NewTableId, User.GetUserId(), ct); 
        return Ok(ApiResponse<object>.Ok(new { }, "Switched")); 
    }
}
