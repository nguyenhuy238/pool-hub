namespace PoolHub.Core.DTOs.Invoice;

public class InvoiceDto { public int InvoiceId { get; set; } public int SessionId { get; set; } public string InvoiceCode { get; set; } = string.Empty; public decimal FinalAmount { get; set; } }
public class CreatePaymentRequest { public int InvoiceId { get; set; } public int PaymentMethodId { get; set; } public decimal Amount { get; set; }
}
