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

    [HttpGet("active")]
    public async Task<ActionResult<ApiResponse<List<ActiveSessionResponse>>>> GetActiveSessions(
        [FromQuery] long? floorId,
        [FromQuery] long? zoneId,
        [FromQuery] long? tableId,
        CancellationToken ct) =>
        Ok(ApiResponse<List<ActiveSessionResponse>>.Ok(await sessionService.GetActiveSessionsAsync(floorId, zoneId, tableId, ct)));

    [HttpGet("by-table/{tableId:long}")]
    public async Task<ActionResult<ApiResponse<SessionDetailDto>>> GetActiveSessionByTable(long tableId, CancellationToken ct) =>
        Ok(ApiResponse<SessionDetailDto>.Ok(await sessionService.GetActiveSessionByTableAsync(tableId, ct)));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<SessionDetailDto>>> GetSessionById(long id, CancellationToken ct) => 
        Ok(ApiResponse<SessionDetailDto>.Ok(await sessionService.GetSessionByIdAsync(id, ct)));

    [HttpGet("{id:long}/summary")]
    public async Task<ActionResult<ApiResponse<SessionSummaryResponse>>> GetSummary(long id, CancellationToken ct) =>
        Ok(ApiResponse<SessionSummaryResponse>.Ok(await sessionService.GetSummaryAsync(id, ct)));

    [HttpGet("{id:long}/time-charges")]
    public async Task<ActionResult<ApiResponse<SessionTimeChargesResponse>>> GetTimeCharges(long id, CancellationToken ct) =>
        Ok(ApiResponse<SessionTimeChargesResponse>.Ok(await sessionService.GetTimeChargesAsync(id, ct)));

    [HttpPost("start")] 
    public async Task<ActionResult<ApiResponse<SessionDto>>> Start([FromBody] StartSessionRequest request, CancellationToken ct) => 
        Ok(ApiResponse<SessionDto>.Ok(await sessionService.StartAsync(User.GetUserId(), request, ct)));

    [HttpPost("{id:long}/close")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff)]
    public async Task<ActionResult<ApiResponse<CloseSessionResponse>>> CloseWithSummary(
        long id,
        [FromBody] CloseSessionRequest request,
        CancellationToken ct) =>
        Ok(ApiResponse<CloseSessionResponse>.Ok(
            await sessionService.CloseWithSummaryAsync(id, User.GetUserId(), request, ct),
            "Session closed"));

    [HttpPost("{id:long}/cancel")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
    public async Task<ActionResult<ApiResponse<SessionDto>>> Cancel(
        long id,
        [FromBody] CancelSessionRequest request,
        CancellationToken ct) =>
        Ok(ApiResponse<SessionDto>.Ok(
            await sessionService.CancelAsync(id, User.GetUserId(), request, ct),
            "Session cancelled"));

    [HttpPost("{id:long}/transfer")]
    public async Task<ActionResult<ApiResponse<TransferTableResponse>>> Transfer(long id, [FromBody] TransferTableRequest request, CancellationToken ct) =>
        Ok(ApiResponse<TransferTableResponse>.Ok(
            await sessionService.TransferTableAsync(id, request, User.GetUserId(), ct),
            "Transferred"));

    [HttpPost("{id:long}/reopen")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff)]
    public async Task<ActionResult<ApiResponse<SessionDetailDto>>> Reopen(long id, [FromBody] ReopenSessionRequest request, CancellationToken ct) =>
        Ok(ApiResponse<SessionDetailDto>.Ok(
            await sessionService.ReopenAsync(id, request, User.GetUserId(), ct),
            "Session reopened"));
}
