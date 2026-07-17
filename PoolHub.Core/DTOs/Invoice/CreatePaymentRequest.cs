namespace PoolHub.Core.DTOs.Invoice;

public class CreatePaymentRequest
{
    public long InvoiceId { get; set; }
    public long PaymentMethodId { get; set; }
    public decimal Amount { get; set; }
    public string? PhoneNumber { get; set; }
    public long? CustomerId { get; set; }
    public string? CustomerName { get; set; }
}
