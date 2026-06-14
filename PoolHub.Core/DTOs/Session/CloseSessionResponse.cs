namespace PoolHub.Core.DTOs.Session;

public class CloseSessionResponse
{
    public long SessionId { get; set; }
    public string SessionCode { get; set; } = string.Empty;
    public int Status { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime EndedAtUtc { get; set; }
    public int TotalDurationMinutes { get; set; }
    public decimal TimeSubtotalAmount { get; set; }
    public long? InvoiceId { get; set; }
    public string? InvoiceCode { get; set; }
    public bool InvoiceGenerated { get; set; }
}
