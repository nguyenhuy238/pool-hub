namespace PoolHub.Core.DTOs.Session;

public class ReleaseSessionTablesResponse
{
    public long SessionId { get; set; }
    public string SessionCode { get; set; } = string.Empty;
    public int SessionStatus { get; set; }
    public List<ReleasedSessionTableDto> ReleasedAssignments { get; set; } = [];
    public List<ActiveSessionTableDto> RemainingActiveAssignments { get; set; } = [];
    public int RemainingActiveTableCount { get; set; }
    public bool WasSessionAutoClosed { get; set; }
    public DateTime? SessionEndedAtUtc { get; set; }
    public decimal TimeSubtotalAmount { get; set; }
    public long? InvoiceId { get; set; }
    public string? InvoiceCode { get; set; }
    public string Message { get; set; } = string.Empty;
}
