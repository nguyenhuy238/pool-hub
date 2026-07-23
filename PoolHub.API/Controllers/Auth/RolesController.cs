using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Roles;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager, Policy = PermissionConstants.RolesManage)]
public class RolesController(IRoleService roleService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get(
        [FromQuery] RoleQueryRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await roleService.GetAsync(request, ct)));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> GetById(long id, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await roleService.GetByIdAsync(id, ct)));

    [HttpGet("permissions")]
    public async Task<ActionResult<ApiResponse<object>>> GetPermissions(CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await roleService.GetPermissionsAsync(ct)));

    [HttpPut("{id:long}/permissions")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<ActionResult<ApiResponse<object>>> SetPermissions(
        long id, [FromBody] UpdateRolePermissionsRequest request, CancellationToken ct)
    {
        var result = await roleService.SetPermissionsAsync(id, request, User.GetUserId(), ct);
        return Ok(ApiResponse<object>.Ok(result, "Role permissions updated."));
    }

    [HttpPost]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        [FromBody] CreateRoleRequest request, CancellationToken ct)
    {
        var result = await roleService.CreateAsync(request, User.GetUserId(), ct);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<object>.Ok(result, "Role created successfully"));
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        long id, [FromBody] UpdateRoleRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(
            await roleService.UpdateAsync(id, request, User.GetUserId(), ct),
            "Role updated successfully"));

    [HttpDelete("{id:long}")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(long id, CancellationToken ct)
    {
        await roleService.DeleteAsync(id, User.GetUserId(), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Role deleted successfully"));
    }
}
