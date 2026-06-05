namespace PoolHub.Core.DTOs.Venue;

public class PricingPlanRuleDto
{
    public long PricingPlanRuleId { get; set; }
    public long PricingPlanId { get; set; }
    public long TableTypeId { get; set; }
    public int DayOfWeek { get; set; }
    public decimal HourlyRate { get; set; }
}
