using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Venue;
using PoolHub.Core.Interfaces;
using PoolHub.Shared;
using PoolHub.Shared.Constants;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/pricing-special-dates")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager, Policy = PermissionConstants.PricingManage)]
public class PricingSpecialDatesController(ICrudService s) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] PaginationRequest r, CancellationToken ct) => 
        Ok(ApiResponse<object>.Ok(await s.GetPricingSpecialDatesAsync(r, ct)));

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> GetById(long id, CancellationToken ct) => 
        Ok(ApiResponse<object>.Ok(await s.GetPricingSpecialDateAsync(id, ct)));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Post(PricingSpecialDateDto dto, CancellationToken ct) => 
        Ok(ApiResponse<object>.Ok(await s.CreatePricingSpecialDateAsync(dto, ct)));

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Put(long id, PricingSpecialDateDto dto, CancellationToken ct) => 
        Ok(ApiResponse<object>.Ok(await s.UpdatePricingSpecialDateAsync(id, dto, ct)));

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(long id, CancellationToken ct)
    {
        await s.DeletePricingSpecialDateAsync(id, ct);
        return Ok(ApiResponse<object>.Ok("Đã xóa thành công."));
    }
}
