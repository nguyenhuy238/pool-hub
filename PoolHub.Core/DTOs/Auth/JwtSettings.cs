namespace PoolHub.Core.DTOs.Auth;

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "PoolHub.API";
    public string Audience { get; set; } = "PoolHub.Client";
    public int AccessTokenExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 7;

    [Obsolete("Use AccessTokenExpirationMinutes.")]
    public int ExpirationHours
    {
        get => Math.Max(1, AccessTokenExpirationMinutes / 60);
        set => AccessTokenExpirationMinutes = value * 60;
    }
}
