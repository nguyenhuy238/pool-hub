using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Customer;

public class CreateCustomerRequest : IValidatableObject
{
    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress, MaxLength(320)]
    public string? Email { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var phone = CustomerPhoneNumberValidation.Normalize(PhoneNumber);
        if (phone is null || !CustomerPhoneNumberValidation.IsValid(phone))
        {
            yield return new ValidationResult(
                CustomerPhoneNumberValidation.ErrorMessage,
                [nameof(PhoneNumber)]);
        }
        else
        {
            PhoneNumber = phone;
        }
    }
}
