using System.Text.RegularExpressions;

namespace PoolHub.Core.DTOs.Customer;

internal static partial class CustomerPhoneNumberValidation
{
    public const string ErrorMessage = "PhoneNumber must be a valid Vietnamese phone number, e.g. 0922222222 or +84922222222.";

    public static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static bool IsValid(string value) => VietnamesePhoneNumberRegex().IsMatch(value);

    [GeneratedRegex(@"^(0[0-9]{9}|\+84[0-9]{9})$")]
    private static partial Regex VietnamesePhoneNumberRegex();
}
