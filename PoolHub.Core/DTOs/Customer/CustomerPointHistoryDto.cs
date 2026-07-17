namespace PoolHub.Core.DTOs.Customer;

public class CustomerPointHistoryDto
{
    public long CustomerPointHistoryId { get; set; }
    public long CustomerId { get; set; }
    public int Points { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long? ReferenceId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
