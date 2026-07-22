namespace PoolHub.Core.DTOs.Session;

public class ReleaseSessionTablesRequest
{
    public List<long> AssignmentIds { get; set; } = [];
    public DateTime? EndedAtUtc { get; set; }
    public string? Note { get; set; }
}
