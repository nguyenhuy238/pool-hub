using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Session;
using PoolHub.Shared;

namespace PoolHub.API.Controllers;

public partial class SessionsController
{
    [HttpGet("{id:long}/summary")]
    public async Task<ActionResult<ApiResponse<SessionSummaryResponse>>> GetSummary(long id, CancellationToken ct) =>
        Ok(ApiResponse<SessionSummaryResponse>.Ok(await _sessionService.GetSummaryAsync(id, ct)));

    [HttpGet("{id:long}/time-charges")]
    public async Task<ActionResult<ApiResponse<SessionTimeChargesResponse>>> GetTimeCharges(long id, CancellationToken ct) =>
        Ok(ApiResponse<SessionTimeChargesResponse>.Ok(await _sessionService.GetTimeChargesAsync(id, ct)));
}
