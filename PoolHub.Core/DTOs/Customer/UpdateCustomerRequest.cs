using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Customer;

public class UpdateCustomerRequest
{
    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Phone, MaxLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress, MaxLength(320)]
    public string? Email { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }
    public bool Status { get; set; } = true;
}
