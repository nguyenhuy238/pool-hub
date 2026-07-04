namespace PoolHub.Core.Entities;

public class PricingPlan : BaseEntity
{
    public long PricingPlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
}
