namespace PoolHub.Core.Entities;

public class CustomerPointHistory : BaseEntity
{
    public long CustomerPointHistoryId { get; set; }
    public long CustomerId { get; set; }
    public int Points { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long? ReferenceId { get; set; }
}
