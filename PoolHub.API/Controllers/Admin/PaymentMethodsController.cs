using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Admin;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers;

[ApiController, Route("api/payment-methods")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff, Policy = PermissionConstants.PaymentsManage)]
public class PaymentMethodsController(IAdminManagementService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<object>>> Get(CancellationToken ct) => Ok(ApiResponse<object>.Ok(await service.GetAllPaymentMethodsAsync(ct)));
    [HttpPost, Authorize(Roles = RoleConstants.Admin)] public async Task<ActionResult<ApiResponse<object>>> Create(UpsertPaymentMethodRequest request, CancellationToken ct) => StatusCode(201, ApiResponse<object>.Ok(await service.CreatePaymentMethodAsync(request, User.GetUserId(), ct)));
    [HttpPut("{id:long}"), Authorize(Roles = RoleConstants.Admin)] public async Task<ActionResult<ApiResponse<object>>> Update(long id, UpsertPaymentMethodRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await service.UpdatePaymentMethodAsync(id, request, User.GetUserId(), ct)));
    [HttpPatch("{id:long}/status")] public async Task<ActionResult<ApiResponse<object>>> Status(long id, UpdateActiveStatusRequest request, CancellationToken ct) { await service.SetPaymentMethodStatusAsync(id, request.IsActive, User.GetUserId(), ct); return Ok(ApiResponse<object>.Ok(new { }, "Payment method status updated.")); }
}
