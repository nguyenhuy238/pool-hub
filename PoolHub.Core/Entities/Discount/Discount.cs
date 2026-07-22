namespace PoolHub.Core.Entities;

public class Discount : BaseEntity
{
    public long DiscountId { get; set; }
    public string DiscountCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal? MaxAmount { get; set; }
    public decimal? MinTimeSubtotal { get; set; }
    public string AppliesTo { get; set; } = "TIME";
    public DateTime StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsVoucher { get; set; } = false;
    public int? PointsRequired { get; set; }
    public long? CustomerId { get; set; }
    public int MaxUsage { get; set; } = 0;
    public int UsageCount { get; set; } = 0;
}
