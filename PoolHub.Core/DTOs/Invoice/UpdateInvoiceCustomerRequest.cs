namespace PoolHub.Core.DTOs.Invoice;

public class UpdateInvoiceCustomerRequest
{
    public long? CustomerId { get; set; }
    public string? PhoneNumber { get; set; }
    public string? FullName { get; set; }
}
