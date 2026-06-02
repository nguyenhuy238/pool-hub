namespace PoolHub.Core.Entities;

public class Discount : BaseEntity
{
    public int DiscountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public int Type { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Invoice : BaseEntity
{
    public int InvoiceId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public int SessionId { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;
    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public int Status { get; set; } = 1;
}

public class InvoiceLine : BaseEntity
{
    public int InvoiceLineId { get; set; }
    public int InvoiceId { get; set; }
    public string LineType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class InvoiceDiscount : BaseEntity
{
    public int InvoiceDiscountId { get; set; }
    public int InvoiceId { get; set; }
    public int DiscountId { get; set; }
    public decimal DiscountAmount { get; set; }
}

public class PaymentMethod : BaseEntity
{
    public int PaymentMethodId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class Payment : BaseEntity
{
    public int PaymentId { get; set; }
    public int InvoiceId { get; set; }
    public int PaymentMethodId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAtUtc { get; set; } = DateTime.UtcNow;
    public int Status { get; set; } = 1;
}
