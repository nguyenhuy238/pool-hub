namespace PoolHub.Core.DTOs.Session;

public class TransferTableResponse
{
    public long SessionId { get; set; }
    public string SessionCode { get; set; } = string.Empty;
    public long FromTableId { get; set; }
    public string FromTableName { get; set; } = string.Empty;
    public long ToTableId { get; set; }
    public string ToTableName { get; set; } = string.Empty;
    public DateTime TransferredAtUtc { get; set; }
    public long CurrentAssignmentId { get; set; }
    public string Message { get; set; } = "Chuyen ban thanh cong";
}
