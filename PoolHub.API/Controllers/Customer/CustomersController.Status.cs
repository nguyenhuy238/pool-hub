using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Customer;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers;

public partial class CustomersController
{
    [HttpPatch("{id:long}/status")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
    public async Task<ActionResult<ApiResponse<object>>> UpdateStatus(
        long id,
        [FromBody] UpdateCustomerStatusRequest request,
        CancellationToken ct)
    {
        await _customerService.UpdateStatusAsync(id, request.Status, User.GetUserId(), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Customer status updated."));
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<ActionResult<ApiResponse<object>>> SoftDelete(long id, CancellationToken ct)
    {
        await _customerService.UpdateStatusAsync(id, false, User.GetUserId(), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Customer soft-deleted."));
    }
}
