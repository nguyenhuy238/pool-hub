using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Dashboard;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Extensions;
using System.Security.Claims;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff + "," + RoleConstants.Customer)]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> Summary(CancellationToken ct) =>
        Ok(ApiResponse<DashboardSummaryDto>.Ok(await dashboardService.GetSummaryAsync(
            User.GetUserId(),
            User.FindAll(ClaimTypes.Role).Select(x => x.Value).ToArray(),
            User.FindAll(PermissionConstants.ClaimType).Select(x => x.Value).ToArray(),
            ct)));
}
