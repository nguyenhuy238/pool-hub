namespace PoolHub.Core.Entities;

public class Customer : BaseEntity
{
    public long CustomerId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Note { get; set; }
    public bool Status { get; set; } = true;
    public int LoyaltyPoints { get; set; } = 0;
    public int TotalPointsEarned { get; set; } = 0;
}
