using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Landing;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers;

[ApiController]
public class PublicLandingPageController(ILandingPageSettingsService service) : ControllerBase
{
    [HttpGet("api/public/landing-page")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LandingPageSettingsDto>>> GetPublic(CancellationToken ct)
        => Ok(ApiResponse<LandingPageSettingsDto>.Ok(await service.GetPublicLandingPageAsync(ct)));

    [HttpGet("api/admin/landing-page-settings")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
    public async Task<ActionResult<ApiResponse<LandingPageSettingsDto>>> GetAdmin(CancellationToken ct)
        => Ok(ApiResponse<LandingPageSettingsDto>.Ok(await service.GetAdminSettingsAsync(ct)));

    [HttpPut("api/admin/landing-page-settings")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<ActionResult<ApiResponse<LandingPageSettingsDto>>> Update([FromBody] LandingPageSettingsDto request, CancellationToken ct)
        => Ok(ApiResponse<LandingPageSettingsDto>.Ok(await service.UpdateSettingsAsync(request, User.GetUserId(), ct), "Landing page settings updated"));

    [HttpPost("api/admin/landing-page-settings/reset-default")]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<ActionResult<ApiResponse<LandingPageSettingsDto>>> ResetDefault(CancellationToken ct)
        => Ok(ApiResponse<LandingPageSettingsDto>.Ok(await service.ResetDefaultAsync(User.GetUserId(), ct), "Landing page settings reset"));
}
