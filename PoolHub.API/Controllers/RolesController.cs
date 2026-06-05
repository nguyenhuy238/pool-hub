using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
public class RolesController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get(CancellationToken ct) => Ok(ApiResponse<object>.Ok(await userService.GetRolesAsync(ct)));
}
