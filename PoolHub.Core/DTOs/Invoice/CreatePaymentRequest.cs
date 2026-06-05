namespace PoolHub.Core.DTOs.Invoice;

public class CreatePaymentRequest
{
    public long InvoiceId { get; set; }
    public long PaymentMethodId { get; set; }
    public decimal Amount { get; set; }
}
