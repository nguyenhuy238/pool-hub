namespace PoolHub.Core.DTOs.Session;

public class SessionTimeChargesResponse
{
    public long SessionId { get; set; }
    public string SessionCode { get; set; } = string.Empty;
    public int Status { get; set; }
    public int ActualDurationMinutes { get; set; }
    public int BillableDurationMinutes { get; set; }
    public int MinimumMinutes { get; set; }
    public int BillingBlockMinutes { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Note { get; set; }
    public List<SessionTimeChargeItemDto> Items { get; set; } = [];
}

public class SessionTimeChargeItemDto
{
    public long AssignmentId { get; set; }
    public long TableId { get; set; }
    public string TableCode { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public long? PricingPlanRuleId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public int DurationMinutes { get; set; }
    public int ActualDurationMinutes { get; set; }
    public int? BillableMinutes { get; set; }
    public decimal HourlyRateSnapshot { get; set; }
    public decimal HourlyRate { get; set; }
    public int MinimumMinutes { get; set; }
    public int BillingBlockMinutes { get; set; }
    public string? PricingPlanName { get; set; }
    public decimal Amount { get; set; }
    public bool IsBillable { get; set; } = true;
    public string? Note { get; set; }
}
