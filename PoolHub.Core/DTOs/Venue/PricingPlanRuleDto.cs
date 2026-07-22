using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Venue;

public class PricingPlanRuleDto
{
    public long PricingPlanRuleId { get; set; }
    public long PricingPlanId { get; set; }
    public long TableTypeId { get; set; }
    [Range(1, 4)]
    public int DayType { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    [Range(typeof(decimal), "0.0001", "999999999999999")]
    public decimal HourlyRate { get; set; }
    [Range(1, 1440)]
    public int MinimumMinutes { get; set; }
    [Range(1, 1440)]
    public int BillingBlockMinutes { get; set; }
}
