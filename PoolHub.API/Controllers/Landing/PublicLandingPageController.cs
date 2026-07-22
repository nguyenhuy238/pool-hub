using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Landing;
using PoolHub.Core.DTOs.Venue;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers;

[ApiController]
public class PublicLandingPageController(ILandingPageSettingsService service, IVenueService venueService) : ControllerBase
{
    [HttpGet("api/public/landing-page")]
    [HttpGet("api/public/landing-settings")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LandingPageSettingsDto>>> GetPublic(CancellationToken ct)
        => Ok(ApiResponse<LandingPageSettingsDto>.Ok(await service.GetPublicLandingPageAsync(ct)));

    [HttpGet("api/public/pricing-summary")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PublicPricingSummaryDto>>> GetPricingSummary(CancellationToken ct)
        => Ok(ApiResponse<PublicPricingSummaryDto>.Ok(await service.GetPricingSummaryAsync(ct)));

    [HttpGet("api/public/deposit-payment-settings")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<DepositPaymentSettingsDto>>> GetDepositPaymentSettings(CancellationToken ct)
        => Ok(ApiResponse<DepositPaymentSettingsDto>.Ok(await service.GetDepositPaymentSettingsAsync(ct)));

    [HttpGet("api/public/venue-layout")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<VenueLayoutResponse>>> GetVenueLayout(CancellationToken ct)
        => Ok(ApiResponse<VenueLayoutResponse>.Ok(await venueService.GetLayoutAsync(ct)));

    [HttpGet("api/public/table-types")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IEnumerable<TableTypeDto>>>> GetTableTypes(CancellationToken ct)
        => Ok(ApiResponse<IEnumerable<TableTypeDto>>.Ok(await venueService.GetTableTypesAsync(ct)));

    [HttpGet("api/admin/landing-page-settings")]
    [HttpGet("api/admin/landing-settings")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager, Policy = PermissionConstants.LandingManage)]
    public async Task<ActionResult<ApiResponse<LandingPageSettingsDto>>> GetAdmin(CancellationToken ct)
        => Ok(ApiResponse<LandingPageSettingsDto>.Ok(await service.GetAdminSettingsAsync(ct)));

    [HttpPut("api/admin/landing-page-settings")]
    [HttpPut("api/admin/landing-settings")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager, Policy = PermissionConstants.LandingManage)]
    public async Task<ActionResult<ApiResponse<LandingPageSettingsDto>>> Update([FromBody] LandingPageSettingsDto request, CancellationToken ct)
        => Ok(ApiResponse<LandingPageSettingsDto>.Ok(await service.UpdateSettingsAsync(request, User.GetUserId(), ct), "Landing page settings updated"));

    [HttpPost("api/admin/landing-page-settings/reset-default")]
    [HttpPost("api/admin/landing-settings/restore-default")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager, Policy = PermissionConstants.LandingManage)]
    public async Task<ActionResult<ApiResponse<LandingPageSettingsDto>>> ResetDefault(CancellationToken ct)
        => Ok(ApiResponse<LandingPageSettingsDto>.Ok(await service.ResetDefaultAsync(User.GetUserId(), ct), "Landing page settings reset"));
}
