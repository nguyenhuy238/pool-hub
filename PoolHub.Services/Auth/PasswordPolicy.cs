using System.Text.RegularExpressions;
using PoolHub.Shared.Exceptions;

namespace PoolHub.Services.Auth;

internal static partial class PasswordPolicy
{
    public static void Validate(string password)
    {
        if (password.Length < 8 ||
            !password.Any(char.IsUpper) ||
            !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit) ||
            !SpecialCharacterRegex().IsMatch(password))
        {
            throw new ValidationException(
                "Password must be at least 8 characters and include uppercase, lowercase, number, and special character.");
        }
    }

    [GeneratedRegex(@"[^a-zA-Z0-9]")]
    private static partial Regex SpecialCharacterRegex();
}
