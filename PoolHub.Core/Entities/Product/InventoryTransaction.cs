namespace PoolHub.Core.Entities;

public class InventoryTransaction : BaseEntity
{
    public long InventoryTransactionId { get; set; }
    public long ProductId { get; set; }
    public int TransactionType { get; set; }
    public int Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public string? ReferenceType { get; set; }
    public long? ReferenceId { get; set; }
    public string? Note { get; set; }
    public long? CreatedByUserId { get; set; }
}
