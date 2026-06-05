namespace PoolHub.Core.Entities;

public class InvoiceDiscount : BaseEntity
{
    public long InvoiceDiscountId { get; set; }
    public long InvoiceId { get; set; }
    public long DiscountId { get; set; }
    public long? AppliedByUserId { get; set; }
    public decimal AmountApplied { get; set; }
    public string? DescriptionSnapshot { get; set; }
}
