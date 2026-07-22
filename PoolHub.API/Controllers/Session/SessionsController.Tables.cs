using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Session;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers;

public partial class SessionsController
{
    [HttpPost("{id:long}/release-tables")]
    [Authorize(Roles = RoleConstants.Operation)]
    public async Task<ActionResult<ApiResponse<ReleaseSessionTablesResponse>>> ReleaseTables(
        long id,
        [FromBody] ReleaseSessionTablesRequest request,
        CancellationToken ct) =>
        Ok(ApiResponse<ReleaseSessionTablesResponse>.Ok(
            await _sessionService.ReleaseTablesAsync(id, request, User.GetUserId(), ct),
            "Tables released"));

    [HttpPost("{id:long}/transfer")]
    public async Task<ActionResult<ApiResponse<TransferTableResponse>>> Transfer(long id, [FromBody] TransferTableRequest request, CancellationToken ct) =>
        Ok(ApiResponse<TransferTableResponse>.Ok(
            await _sessionService.TransferTableAsync(id, request, User.GetUserId(), ct),
            "Transferred"));
}
