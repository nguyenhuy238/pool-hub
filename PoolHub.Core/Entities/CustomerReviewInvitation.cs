namespace PoolHub.Core.Entities;

public class CustomerReviewInvitation : BaseEntity
{
    public long CustomerReviewInvitationId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string TokenHash { get; set; } = string.Empty;
    public long? CustomerId { get; set; }
    public long SessionId { get; set; }
    public long InvoiceId { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public long? CreatedByUserId { get; set; }
    public int Status { get; set; } = 1;
}
