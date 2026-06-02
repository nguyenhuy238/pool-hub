namespace PoolHub.Core.DTOs.Invoice;

public class InvoiceDto { public long InvoiceId { get; set; } public long SessionId { get; set; } public string InvoiceCode { get; set; } = string.Empty; public decimal GrandTotalAmount { get; set; } }
public class CreatePaymentRequest { public long InvoiceId { get; set; } public long PaymentMethodId { get; set; } public decimal Amount { get; set; }
}
