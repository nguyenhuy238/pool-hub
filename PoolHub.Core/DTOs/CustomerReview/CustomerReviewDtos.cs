using PoolHub.Core.DTOs.Common;

namespace PoolHub.Core.DTOs.CustomerReview;

public class CustomerReviewDto
{
    public Guid PublicId { get; set; }
    public long? CustomerId { get; set; }
    public Guid? CustomerPublicId { get; set; }
    public string? CustomerName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? BookingCode { get; set; }
    public string? SessionCode { get; set; }
    public string? InvoiceCode { get; set; }
    public int Rating { get; set; }
    public string Content { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? CheckInImageUrl { get; set; }
    public int Status { get; set; }
    public bool IsFeatured { get; set; }
    public int DisplayOrder { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? RejectedReason { get; set; }
    public string? Note { get; set; }
}

public class PublicReviewDto
{
    public Guid PublicId { get; set; }
    public int Rating { get; set; }
    public string Content { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? CheckInImageUrl { get; set; }
    public bool IsFeatured { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class CustomerReviewQueryRequest : PaginationRequest
{
    public int? Status { get; set; }
    public int? Rating { get; set; }
    public long? CustomerId { get; set; }
    public Guid? CustomerPublicId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class PublicReviewQueryRequest : PaginationRequest
{
    public bool? FeaturedOnly { get; set; }
    public int? MinRating { get; set; }
}

public class CreatePublicReviewRequest
{
    public string? BookingCode { get; set; }
    public string? SessionCode { get; set; }
    public string? InvoiceCode { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public int Rating { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? CheckInImageUrl { get; set; }
}

public class ApproveCustomerReviewRequest
{
    public bool? IsFeatured { get; set; }
    public int? DisplayOrder { get; set; }
}

public class RejectCustomerReviewRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class UpdateCustomerReviewVisibilityRequest
{
    public int Status { get; set; }
    public bool? IsFeatured { get; set; }
    public int? DisplayOrder { get; set; }
}

public class UpdateCustomerReviewRequest
{
    public string Content { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? CheckInImageUrl { get; set; }
    public bool IsFeatured { get; set; }
    public int DisplayOrder { get; set; }
    public string? Note { get; set; }
}

public class ReviewInvitationDto
{
    public Guid PublicId { get; set; }
    public string CustomerDisplayName { get; set; } = string.Empty;
    public string InvoiceCode { get; set; } = string.Empty;
    public string SessionCode { get; set; } = string.Empty;
    public DateTime? PlayedAt { get; set; }
    public string? TableName { get; set; }
    public bool CanSubmit { get; set; }
    public string? Reason { get; set; }
}

public class ReviewInvitationLinkDto
{
    public Guid PublicId { get; set; }
    public string ReviewUrl { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}

public class SubmitReviewInvitationRequest
{
    public int Rating { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? CheckInImageUrl { get; set; }
}
