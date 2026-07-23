namespace PoolHub.Core.DTOs.Customer;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Public-facing customer profile. Internal notes and administrative status are
/// intentionally excluded from this contract.
/// </summary>
public class CustomerPortalProfileDto
{
    public long CustomerId { get; set; }
    public Guid PublicId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int LoyaltyPoints { get; set; }
    public int TotalPointsEarned { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class UpdateCustomerPortalProfileRequest : IValidatableObject
{
    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var phone = CustomerPhoneNumberValidation.Normalize(PhoneNumber);
        if (phone is null || !CustomerPhoneNumberValidation.IsValid(phone))
        {
            yield return new ValidationResult(
                CustomerPhoneNumberValidation.ErrorMessage,
                [nameof(PhoneNumber)]);
            yield break;
        }

        PhoneNumber = phone;
    }
}
