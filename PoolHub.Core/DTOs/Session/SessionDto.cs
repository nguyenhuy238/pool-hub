namespace PoolHub.Core.DTOs.Session;

public class SessionDto
{
    public long SessionId { get; set; }
    public string SessionCode { get; set; } = string.Empty;
    public int Status { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public string TableName { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public List<SessionTableAssignmentDto> Assignments { get; set; } = [];
}
