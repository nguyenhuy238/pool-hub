using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Session;
using PoolHub.Shared;

namespace PoolHub.API.Controllers;

public partial class SessionsController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<SessionDto>>>> GetSessions([FromQuery] SessionQueryRequest request, CancellationToken ct) =>
        Ok(ApiResponse<PagedResult<SessionDto>>.Ok(await _sessionService.GetSessionsAsync(request, ct)));

    [HttpGet("active")]
    public async Task<ActionResult<ApiResponse<List<ActiveSessionResponse>>>> GetActiveSessions(
        [FromQuery] long? floorId,
        [FromQuery] long? zoneId,
        [FromQuery] long? tableId,
        CancellationToken ct) =>
        Ok(ApiResponse<List<ActiveSessionResponse>>.Ok(await _sessionService.GetActiveSessionsAsync(floorId, zoneId, tableId, ct)));

    [HttpGet("by-table/{tableId:long}")]
    public async Task<ActionResult<ApiResponse<SessionDetailDto>>> GetActiveSessionByTable(long tableId, CancellationToken ct) =>
        Ok(ApiResponse<SessionDetailDto>.Ok(await _sessionService.GetActiveSessionByTableAsync(tableId, ct)));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<SessionDetailDto>>> GetSessionById(long id, CancellationToken ct) =>
        Ok(ApiResponse<SessionDetailDto>.Ok(await _sessionService.GetSessionByIdAsync(id, ct)));
}
