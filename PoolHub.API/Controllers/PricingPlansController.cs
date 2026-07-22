using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Venue;
using PoolHub.Core.Interfaces;
using PoolHub.Shared;
using PoolHub.Shared.Constants;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/pricing-plans")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager, Policy = PermissionConstants.PricingManage)]
public class PricingPlansController(ICrudService s) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<object>>> GetPlans([FromQuery] PricingPlanPaginationRequest r, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetPricingPlansAsync(r, ct)));
    [HttpGet("rules")] public async Task<ActionResult<ApiResponse<object>>> GetRules([FromQuery] PricingRulePaginationRequest r, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetPricingPlanRulesAsync(r, ct)));
    [HttpGet("{id}")] public async Task<ActionResult<ApiResponse<object>>> Get(long id, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetPricingPlanAsync(id, ct)));
    [HttpPost] public async Task<ActionResult<ApiResponse<object>>> Create(PricingPlanDto d, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.CreatePricingPlanAsync(d, ct)));
    [HttpPut("{id}")] public async Task<ActionResult<ApiResponse<object>>> Update(long id, PricingPlanDto d, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.UpdatePricingPlanAsync(id, d, ct)));
    [HttpDelete("{id}")] public async Task<ActionResult<ApiResponse<object?>>> Delete(long id, CancellationToken ct) { await s.DeletePricingPlanAsync(id, ct); return Ok(ApiResponse<object?>.Ok(null)); }
    
    [HttpGet("{planId}/rules/{ruleId}")] public async Task<ActionResult<ApiResponse<object>>> GetRule(long planId, long ruleId, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetPricingPlanRuleAsync(ruleId, ct)));
    [HttpPost("{planId}/rules")] public async Task<ActionResult<ApiResponse<object>>> CreateRule(long planId, PricingPlanRuleDto d, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.CreatePricingPlanRuleAsync(planId, d, ct)));
    [HttpPut("{planId}/rules/{ruleId}")] public async Task<ActionResult<ApiResponse<object>>> UpdateRule(long planId, long ruleId, PricingPlanRuleDto d, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.UpdatePricingPlanRuleAsync(planId, ruleId, d, ct)));
    [HttpDelete("{planId}/rules/{ruleId}")] public async Task<ActionResult<ApiResponse<object?>>> DeleteRule(long planId, long ruleId, CancellationToken ct) { await s.DeletePricingPlanRuleAsync(planId, ruleId, ct); return Ok(ApiResponse<object?>.Ok(null)); }
}
