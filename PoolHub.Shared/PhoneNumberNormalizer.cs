namespace PoolHub.Shared;

public static class PhoneNumberNormalizer
{
    public static string Normalize(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber)) return string.Empty;

        var chars = phoneNumber.Trim()
            .Where(c => !char.IsWhiteSpace(c) && c is not '-' and not '.' and not '(' and not ')')
            .ToArray();

        return new string(chars);
    }
}
