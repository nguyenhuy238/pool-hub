using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Customer;

public class UpdateCustomerRequest : IValidatableObject
{
    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    [EmailAddress, MaxLength(320)]
    public string? Email { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }
    public bool Status { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var phone = CustomerPhoneNumberValidation.Normalize(PhoneNumber);
        if (phone is null)
        {
            PhoneNumber = null;
            yield break;
        }

        if (!CustomerPhoneNumberValidation.IsValid(phone))
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
