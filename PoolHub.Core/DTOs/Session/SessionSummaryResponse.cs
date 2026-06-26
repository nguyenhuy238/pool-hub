namespace PoolHub.Core.DTOs.Session;

public class SessionSummaryResponse
{
    public long SessionId { get; set; }
    public string SessionCode { get; set; } = string.Empty;
    public int Status { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public int CurrentDurationMinutes { get; set; }
    public decimal TimeSubtotalAmount { get; set; }
    public decimal OrderSubtotalAmount { get; set; }
    public decimal SubtotalAmount { get; set; }
    public SessionSummaryTableDto? CurrentTable { get; set; }
    public List<SessionSummaryAssignmentDto> Assignments { get; set; } = [];
}

public class SessionSummaryAssignmentDto
{
    public long SessionTableAssignmentId { get; set; }
    public long TableId { get; set; }
    public string TableCode { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public int DurationMinutes { get; set; }
    public decimal HourlyRateSnapshot { get; set; }
    public decimal Amount { get; set; }
    public bool IsCurrent { get; set; }
}

public class SessionSummaryTableDto
{
    public long TableId { get; set; }
    public string TableCode { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
}
