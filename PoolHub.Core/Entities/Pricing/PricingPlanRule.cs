namespace PoolHub.Core.Entities;

public class PricingPlanRule : BaseEntity
{
    public long PricingPlanRuleId { get; set; }
    public long PricingPlanId { get; set; }
    public long TableTypeId { get; set; }
    public int DayType { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public decimal HourlyRate { get; set; }
    public int MinimumMinutes { get; set; }
    public int BillingBlockMinutes { get; set; }
    public bool IsActive { get; set; } = true;
}
