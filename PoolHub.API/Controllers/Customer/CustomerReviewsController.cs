using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.CustomerReview;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers;

[ApiController]
public class CustomerReviewsController(ICustomerReviewService service) : ControllerBase
{
    [HttpGet("api/public/reviews")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> GetPublicReviews([FromQuery] PublicReviewQueryRequest request, CancellationToken ct)
        => Ok(ApiResponse<object>.Ok(await service.GetPublicReviewsAsync(request, ct)));

    [HttpPost("api/public/reviews")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> CreatePublicReview([FromBody] CreatePublicReviewRequest request, CancellationToken ct)
        => StatusCode(StatusCodes.Status201Created,
            ApiResponse<object>.Ok(await service.CreatePublicReviewAsync(request, ct), "Cảm ơn bạn đã chia sẻ trải nghiệm tại PoolHub."));

    [HttpGet("api/public/reviews/invitations/{token}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<ReviewInvitationDto>>> GetInvitation(string token, CancellationToken ct)
        => Ok(ApiResponse<ReviewInvitationDto>.Ok(await service.GetInvitationAsync(token, ct)));

    [HttpPost("api/public/reviews/invitations/{token}/submit")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> SubmitInvitation(string token, [FromBody] SubmitReviewInvitationRequest request, CancellationToken ct)
        => StatusCode(StatusCodes.Status201Created,
            ApiResponse<object>.Ok(await service.SubmitInvitationAsync(token, request, ct), "Cảm ơn bạn đã chia sẻ trải nghiệm tại PoolHub."));

    [HttpGet("api/customer-reviews")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
    public async Task<ActionResult<ApiResponse<object>>> GetReviews([FromQuery] CustomerReviewQueryRequest request, CancellationToken ct)
        => Ok(ApiResponse<object>.Ok(await service.GetReviewsAsync(request, ct)));

    [HttpGet("api/customer-reviews/{publicId:guid}")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff)]
    public async Task<ActionResult<ApiResponse<object>>> GetReview(Guid publicId, CancellationToken ct)
        => Ok(ApiResponse<object>.Ok(await service.GetReviewAsync(publicId, ct)));

    [HttpPatch("api/customer-reviews/{publicId:guid}/approve")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
    public async Task<ActionResult<ApiResponse<object>>> Approve(Guid publicId, [FromBody] ApproveCustomerReviewRequest request, CancellationToken ct)
        => Ok(ApiResponse<object>.Ok(await service.ApproveAsync(publicId, request, User.GetUserId(), ct), "Review approved."));

    [HttpPatch("api/customer-reviews/{publicId:guid}/reject")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
    public async Task<ActionResult<ApiResponse<object>>> Reject(Guid publicId, [FromBody] RejectCustomerReviewRequest request, CancellationToken ct)
        => Ok(ApiResponse<object>.Ok(await service.RejectAsync(publicId, request, User.GetUserId(), ct), "Review rejected."));

    [HttpPatch("api/customer-reviews/{publicId:guid}/visibility")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
    public async Task<ActionResult<ApiResponse<object>>> UpdateVisibility(Guid publicId, [FromBody] UpdateCustomerReviewVisibilityRequest request, CancellationToken ct)
        => Ok(ApiResponse<object>.Ok(await service.UpdateVisibilityAsync(publicId, request, User.GetUserId(), ct), "Review visibility updated."));

    [HttpPut("api/customer-reviews/{publicId:guid}")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
    public async Task<ActionResult<ApiResponse<object>>> Update(Guid publicId, [FromBody] UpdateCustomerReviewRequest request, CancellationToken ct)
        => Ok(ApiResponse<object>.Ok(await service.UpdateAsync(publicId, request, User.GetUserId(), ct), "Review updated."));

    [HttpDelete("api/customer-reviews/{publicId:guid}")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
    public async Task<ActionResult<ApiResponse<object>>> Hide(Guid publicId, CancellationToken ct)
    {
        await service.HideAsync(publicId, User.GetUserId(), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Review hidden."));
    }

    [HttpPost("api/customer-reviews/invitations/invoice/{invoiceId:long}")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff)]
    public async Task<ActionResult<ApiResponse<ReviewInvitationLinkDto>>> CreateInvitationForInvoice(long invoiceId, CancellationToken ct)
        => Ok(ApiResponse<ReviewInvitationLinkDto>.Ok(await service.CreateInvitationForInvoiceAsync(invoiceId, User.GetUserId(), ct), "Review invitation created."));
}
