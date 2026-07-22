using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Admin;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers;

[ApiController, Route("api/discounts")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Cashier, Policy = PermissionConstants.DiscountsManage)]
public class DiscountsController(IAdminManagementService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] DiscountQueryRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await service.GetDiscountsAsync(request, ct)));
    [HttpPost, Authorize(Roles = RoleConstants.Admin)] public async Task<ActionResult<ApiResponse<object>>> Create(UpsertDiscountRequest request, CancellationToken ct) => StatusCode(201, ApiResponse<object>.Ok(await service.CreateDiscountAsync(request, User.GetUserId(), ct)));
    [HttpPut("{id:long}"), Authorize(Roles = RoleConstants.Admin)] public async Task<ActionResult<ApiResponse<object>>> Update(long id, UpsertDiscountRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await service.UpdateDiscountAsync(id, request, User.GetUserId(), ct)));
    [HttpPatch("{id:long}/status"), Authorize(Roles = RoleConstants.Admin)] public async Task<ActionResult<ApiResponse<object>>> Status(long id, UpdateActiveStatusRequest request, CancellationToken ct) { await service.SetDiscountStatusAsync(id, request.IsActive, User.GetUserId(), ct); return Ok(ApiResponse<object>.Ok(new { }, "Discount status updated.")); }
    [HttpPost("validate")] public async Task<ActionResult<ApiResponse<object>>> Validate(ValidateDiscountRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await service.ValidateDiscountAsync(request, ct)));
}
