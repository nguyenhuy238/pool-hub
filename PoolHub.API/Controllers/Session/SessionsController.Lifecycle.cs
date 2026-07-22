using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Session;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers;

public partial class SessionsController
{
    [HttpPost("start")]
    public async Task<ActionResult<ApiResponse<SessionDto>>> Start([FromBody] StartSessionRequest request, CancellationToken ct) =>
        Ok(ApiResponse<SessionDto>.Ok(await _sessionService.StartAsync(User.GetUserId(), request, ct)));

    [HttpPost("{id:long}/close")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff)]
    public async Task<ActionResult<ApiResponse<CloseSessionResponse>>> CloseWithSummary(
        long id,
        [FromBody] CloseSessionRequest request,
        CancellationToken ct) =>
        Ok(ApiResponse<CloseSessionResponse>.Ok(
            await _sessionService.CloseWithSummaryAsync(id, User.GetUserId(), request, ct),
            "Session closed"));

    [HttpPost("{id:long}/cancel")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
    public async Task<ActionResult<ApiResponse<SessionDto>>> Cancel(
        long id,
        [FromBody] CancelSessionRequest request,
        CancellationToken ct) =>
        Ok(ApiResponse<SessionDto>.Ok(
            await _sessionService.CancelAsync(id, User.GetUserId(), request, ct),
            "Session cancelled"));

    [HttpPost("{id:long}/reopen")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff)]
    public async Task<ActionResult<ApiResponse<SessionDetailDto>>> Reopen(long id, [FromBody] ReopenSessionRequest request, CancellationToken ct) =>
        Ok(ApiResponse<SessionDetailDto>.Ok(
            await _sessionService.ReopenAsync(id, request, User.GetUserId(), ct),
            "Session reopened"));
}
