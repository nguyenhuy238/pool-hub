namespace PoolHub.Core.Entities;

public class Payment : BaseEntity
{
    public long PaymentId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long InvoiceId { get; set; }
    public long PaymentMethodId { get; set; }
    public decimal Amount { get; set; }
    public int PaymentStatus { get; set; } = 1;
    public string? TransactionCode { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public long? ReceivedByUserId { get; set; }
    public string? Note { get; set; }
}
