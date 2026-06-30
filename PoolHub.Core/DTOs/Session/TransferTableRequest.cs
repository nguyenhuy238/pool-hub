namespace PoolHub.Core.DTOs.Session;

public class TransferTableRequest
{
    public long ToTableId { get; set; }
    public long NewTableId { get; set; }
    public string? Reason { get; set; }
    public string? Note { get; set; }
    public bool MarkOldTableMaintenance { get; set; }

    public long TargetTableId => ToTableId > 0 ? ToTableId : NewTableId;
}
