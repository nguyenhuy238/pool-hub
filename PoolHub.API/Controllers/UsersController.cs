using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Users;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] PaginationRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await userService.GetUsersAsync(request, ct)));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> GetById(int id, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await userService.GetByIdAsync(id, ct)));

    [HttpPost]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] CreateUserRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await userService.CreateAsync(request, ct)));

    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] UpdateUserRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await userService.UpdateAsync(id, request, ct)));

    [HttpPut("{id:int}/roles")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<ActionResult<ApiResponse<object>>> UpdateRoles(int id, [FromBody] UpdateUserRoleRequest request, CancellationToken ct)
    { await userService.UpdateRolesAsync(id, request, ct); return Ok(ApiResponse<object>.Ok(new { }, "Roles updated")); }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<ActionResult<ApiResponse<object>>> UpdateStatus(int id, [FromBody] UpdateUserStatusRequest request, CancellationToken ct)
    { await userService.UpdateStatusAsync(id, request.Status, ct); return Ok(ApiResponse<object>.Ok(new { }, "Status updated")); }
}
