using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Users;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get(
        [FromQuery] UserQueryRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await userService.GetUsersAsync(request, ct)));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> GetById(long id, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await userService.GetByIdAsync(id, ct)));

    [HttpPost]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        [FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var result = await userService.CreateAsync(request, User.GetUserId(), ct);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<object>.Ok(result, "User created successfully"));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        long id, [FromBody] UpdateUserRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(
            await userService.UpdateAsync(id, request, User.GetUserId(), ct),
            "User updated successfully"));

    [HttpPost("{id:long}/roles")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<ActionResult<ApiResponse<object>>> AssignRoles(
        long id, [FromBody] UpdateUserRoleRequest request, CancellationToken ct)
    {
        await userService.AssignRolesAsync(id, request, User.GetUserId(), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Roles assigned successfully"));
    }

    [HttpPut("{id:long}/roles")]
    [Authorize(Roles = RoleConstants.Admin)]
    [ApiExplorerSettings(IgnoreApi = true)]
    public Task<ActionResult<ApiResponse<object>>> AssignRolesCompatibility(
        long id, [FromBody] UpdateUserRoleRequest request, CancellationToken ct) =>
        AssignRoles(id, request, ct);

    [HttpDelete("{id:long}/roles/{roleId:long}")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<ActionResult<ApiResponse<object>>> RemoveRole(
        long id, long roleId, CancellationToken ct)
    {
        await userService.RemoveRoleAsync(id, roleId, User.GetUserId(), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Role removed successfully"));
    }

    [HttpPatch("{id:long}/status")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<ActionResult<ApiResponse<object>>> UpdateStatus(
        long id, [FromBody] UpdateUserStatusRequest request, CancellationToken ct)
    {
        await userService.UpdateStatusAsync(id, request.Status, User.GetUserId(), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Status updated successfully"));
    }
}
