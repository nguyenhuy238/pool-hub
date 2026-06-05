using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.Interfaces;
using PoolHub.Core.DTOs.Venue;
using PoolHub.Shared;
using PoolHub.Shared.Constants;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/pricing-plans")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
public class PricingPlansController(ICrudService s) : ControllerBase
{
    /// <summary>
    /// Lấy danh sách khung giá (Pricing Plans) có phân trang.
    /// </summary>
    /// <param name="r">Các tham số phân trang.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Danh sách khung giá.</returns>
    [HttpGet] [AllowAnonymous] public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] PaginationRequest r, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetPricingPlansAsync(r, ct)));
    
    /// <summary>
    /// Lấy thông tin chi tiết một khung giá theo ID.
    /// </summary>
    /// <param name="id">ID của khung giá.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Chi tiết khung giá.</returns>
    [HttpGet("{id:int}")] [AllowAnonymous] public async Task<ActionResult<ApiResponse<object>>> GetById(int id, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetPricingPlanAsync(id, ct)));
    
    /// <summary>
    /// Tạo mới một khung giá.
    /// </summary>
    /// <remarks>
    /// Request mẫu:
    ///
    ///     POST /api/pricing-plans
    ///     {
    ///        "name": "Weekend Plan",
    ///        "isDefault": false,
    ///        "isActive": true
    ///     }
    /// </remarks>
    /// <param name="dto">Thông tin khung giá.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Khung giá vừa tạo.</returns>
    [HttpPost] public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] PricingPlanDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.CreatePricingPlanAsync(dto, ct)));
    
    /// <summary>
    /// Cập nhật thông tin một khung giá.
    /// </summary>
    /// <param name="id">ID của khung giá.</param>
    /// <param name="dto">Dữ liệu cần cập nhật.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Khung giá đã cập nhật.</returns>
    [HttpPut("{id:int}")] public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] PricingPlanDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.UpdatePricingPlanAsync(id, dto, ct)));
    
    /// <summary>
    /// Xóa một khung giá.
    /// </summary>
    /// <param name="id">ID của khung giá.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Thông báo thành công.</returns>
    [HttpDelete("{id:int}")] public async Task<ActionResult<ApiResponse<object>>> Delete(int id, CancellationToken ct) { await s.DeletePricingPlanAsync(id, ct); return Ok(ApiResponse<object>.Ok(new { }, "Deleted")); }

    /// <summary>
    /// Lấy danh sách quy tắc tính tiền (Pricing Plan Rules) có phân trang.
    /// </summary>
    /// <param name="r">Các tham số phân trang.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Danh sách quy tắc tính tiền.</returns>
    [HttpGet("rules")] [AllowAnonymous] public async Task<ActionResult<ApiResponse<object>>> GetRules([FromQuery] PaginationRequest r, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetPricingPlanRulesAsync(r, ct)));
    
    /// <summary>
    /// Lấy thông tin chi tiết một quy tắc tính tiền theo ID.
    /// </summary>
    /// <param name="planId">ID của khung giá.</param>
    /// <param name="ruleId">ID của quy tắc.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Chi tiết quy tắc tính tiền.</returns>
    [HttpGet("{planId:int}/rules/{ruleId:int}")] [AllowAnonymous] public async Task<ActionResult<ApiResponse<object>>> GetRuleById(int planId, int ruleId, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetPricingPlanRuleAsync(ruleId, ct)));
    
    /// <summary>
    /// Tạo mới một quy tắc tính tiền thuộc một khung giá.
    /// </summary>
    /// <remarks>
    /// Request mẫu:
    ///
    ///     POST /api/pricing-plans/1/rules
    ///     {
    ///        "tableTypeId": 1,
    ///        "dayOfWeek": 0,
    ///        "startTime": "08:00:00",
    ///        "endTime": "18:00:00",
    ///        "hourlyRate": 100000,
    ///        "minimumMinutes": 30,
    ///        "billingBlockMinutes": 15
    ///     }
    /// </remarks>
    /// <param name="planId">ID của khung giá.</param>
    /// <param name="dto">Dữ liệu quy tắc mới.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Quy tắc vừa tạo.</returns>
    [HttpPost("{planId:int}/rules")] public async Task<ActionResult<ApiResponse<object>>> CreateRule(int planId, [FromBody] PricingPlanRuleDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.CreatePricingPlanRuleAsync(planId, dto, ct)));
    
    /// <summary>
    /// Cập nhật thông tin một quy tắc tính tiền.
    /// </summary>
    /// <param name="planId">ID của khung giá.</param>
    /// <param name="ruleId">ID của quy tắc cần cập nhật.</param>
    /// <param name="dto">Dữ liệu cần cập nhật.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Quy tắc đã cập nhật.</returns>
    [HttpPut("{planId:int}/rules/{ruleId:int}")] public async Task<ActionResult<ApiResponse<object>>> UpdateRule(int planId, int ruleId, [FromBody] PricingPlanRuleDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.UpdatePricingPlanRuleAsync(planId, ruleId, dto, ct)));
    
    /// <summary>
    /// Xóa một quy tắc tính tiền.
    /// </summary>
    /// <param name="planId">ID của khung giá.</param>
    /// <param name="ruleId">ID của quy tắc.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Thông báo thành công.</returns>
    [HttpDelete("{planId:int}/rules/{ruleId:int}")] public async Task<ActionResult<ApiResponse<object>>> DeleteRule(int planId, int ruleId, CancellationToken ct) { await s.DeletePricingPlanRuleAsync(planId, ruleId, ct); return Ok(ApiResponse<object>.Ok(new { }, "Deleted")); }
}
