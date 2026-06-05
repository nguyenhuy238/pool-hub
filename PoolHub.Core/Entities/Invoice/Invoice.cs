namespace PoolHub.Core.Entities;

public class Invoice : BaseEntity
{
    public long InvoiceId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string InvoiceCode { get; set; } = string.Empty;
    public long SessionId { get; set; }
    public long? CustomerId { get; set; }
    public decimal TimeSubtotalAmount { get; set; }
    public decimal ProductSubtotalAmount { get; set; }
    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrandTotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public int PaymentStatus { get; set; } = 1;
    public int Status { get; set; } = 1;
    public long? IssuedByUserId { get; set; }
    public DateTime? IssuedAtUtc { get; set; }
    public string? Note { get; set; }
}
