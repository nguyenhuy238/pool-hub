namespace PoolHub.Core.Entities;

public class CustomerReview : BaseEntity
{
    public long CustomerReviewId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long? CustomerId { get; set; }
    public long? BookingId { get; set; }
    public long? SessionId { get; set; }
    public long? InvoiceId { get; set; }
    public int Rating { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? CheckInImageUrl { get; set; }
    public int Status { get; set; } = 1;
    public bool IsFeatured { get; set; }
    public int DisplayOrder { get; set; }
    public string Source { get; set; } = "Public";
    public long? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? RejectedReason { get; set; }
    public string? Note { get; set; }
}
