namespace PoolHub.Core.DTOs.Session;

public class CloseSessionRequest
{
    public DateTime? EndedAtUtc { get; set; }
    public bool GenerateInvoice { get; set; } = true;
}
