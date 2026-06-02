using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Venue;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/pricing-plans")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
public class PricingPlansController(IVenueService s) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] PaginationRequest r, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetPricingPlansAsync(r, ct)));
    [HttpGet("{id:int}")] public async Task<ActionResult<ApiResponse<object>>> GetById(int id, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetPricingPlanAsync(id, ct)));
    [HttpPost] public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] PricingPlanDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.CreatePricingPlanAsync(dto, ct)));
    [HttpPut("{id:int}")] public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] PricingPlanDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.UpdatePricingPlanAsync(id, dto, ct)));
    [HttpDelete("{id:int}")] public async Task<ActionResult<ApiResponse<object>>> Delete(int id, CancellationToken ct) { await s.DeletePricingPlanAsync(id, ct); return Ok(ApiResponse<object>.Ok(new { }, "Deleted")); }

    [HttpGet("rules")] public async Task<ActionResult<ApiResponse<object>>> GetRules([FromQuery] PaginationRequest r, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetPricingPlanRulesAsync(r, ct)));
    [HttpGet("rules/{ruleId:int}")] public async Task<ActionResult<ApiResponse<object>>> GetRuleById(int ruleId, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetPricingPlanRuleAsync(ruleId, ct)));
    [HttpPost("rules")] public async Task<ActionResult<ApiResponse<object>>> CreateRule([FromBody] PricingPlanRuleDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.CreatePricingPlanRuleAsync(dto, ct)));
    [HttpPut("rules/{ruleId:int}")] public async Task<ActionResult<ApiResponse<object>>> UpdateRule(int ruleId, [FromBody] PricingPlanRuleDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.UpdatePricingPlanRuleAsync(ruleId, dto, ct)));
    [HttpDelete("rules/{ruleId:int}")] public async Task<ActionResult<ApiResponse<object>>> DeleteRule(int ruleId, CancellationToken ct) { await s.DeletePricingPlanRuleAsync(ruleId, ct); return Ok(ApiResponse<object>.Ok(new { }, "Deleted")); }
}
