namespace PoolHub.Core.DTOs.Customer;

public class UpdateCustomerRequest
{
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Note { get; set; }
    public bool Status { get; set; } = true;
}
