namespace PoolHub.Core.DTOs.Session;

public class SessionTableAssignmentDto
{
    public long SessionTableAssignmentId { get; set; }
    public long SessionId { get; set; }
    public long TableId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string TableCode { get; set; } = string.Empty;
    public long? PricingPlanRuleId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public int? DurationMinutes { get; set; }
    public decimal HourlyRateSnapshot { get; set; }
    public decimal? Amount { get; set; }
    public long? AssignedByUserId { get; set; }
    public string? Note { get; set; }
}
