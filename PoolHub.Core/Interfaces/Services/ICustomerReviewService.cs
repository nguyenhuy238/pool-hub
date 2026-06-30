using PoolHub.Core.DTOs.CustomerReview;
using PoolHub.Shared;

namespace PoolHub.Core.Interfaces.Services;

public interface ICustomerReviewService
{
    Task<PagedResult<PublicReviewDto>> GetPublicReviewsAsync(PublicReviewQueryRequest request, CancellationToken ct);
    Task<CustomerReviewDto> CreatePublicReviewAsync(CreatePublicReviewRequest request, CancellationToken ct);
    Task<PagedResult<CustomerReviewDto>> GetReviewsAsync(CustomerReviewQueryRequest request, CancellationToken ct);
    Task<CustomerReviewDto> GetReviewAsync(Guid publicId, CancellationToken ct);
    Task<CustomerReviewDto> ApproveAsync(Guid publicId, ApproveCustomerReviewRequest request, long actorUserId, CancellationToken ct);
    Task<CustomerReviewDto> RejectAsync(Guid publicId, RejectCustomerReviewRequest request, long actorUserId, CancellationToken ct);
    Task<CustomerReviewDto> UpdateVisibilityAsync(Guid publicId, UpdateCustomerReviewVisibilityRequest request, long actorUserId, CancellationToken ct);
    Task<CustomerReviewDto> UpdateAsync(Guid publicId, UpdateCustomerReviewRequest request, long actorUserId, CancellationToken ct);
    Task HideAsync(Guid publicId, long actorUserId, CancellationToken ct);
    Task<ReviewInvitationLinkDto> CreateInvitationForInvoiceAsync(long invoiceId, long? actorUserId, CancellationToken ct);
    Task<ReviewInvitationDto> GetInvitationAsync(string token, CancellationToken ct);
    Task<CustomerReviewDto> SubmitInvitationAsync(string token, SubmitReviewInvitationRequest request, CancellationToken ct);
}
