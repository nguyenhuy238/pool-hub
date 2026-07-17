using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.Interfaces;
using PoolHub.Shared;
using PoolHub.Shared.Constants;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/pricing-plans")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager, Policy = PermissionConstants.PricingManage)]
public class PricingPlansController(ICrudService s) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] PaginationRequest r, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetPricingPlansAsync(r, ct)));
    [HttpGet("rules")] public async Task<ActionResult<ApiResponse<object>>> GetRules([FromQuery] PaginationRequest r, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetPricingPlanRulesAsync(r, ct)));
}
