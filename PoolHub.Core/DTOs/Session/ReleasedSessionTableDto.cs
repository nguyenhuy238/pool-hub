namespace PoolHub.Core.DTOs.Session;

public class ReleasedSessionTableDto
{
    public long AssignmentId { get; set; }
    public long TableId { get; set; }
    public string TableCode { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime EndedAtUtc { get; set; }
    public int DurationMinutes { get; set; }
    public decimal HourlyRateSnapshot { get; set; }
    public decimal Amount { get; set; }
}
