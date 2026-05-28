namespace PoolHub.Core.DTOs.Auth;

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "PoolHub.API";
    public string Audience { get; set; } = "PoolHub.Client";
    public int ExpirationHours { get; set; } = 8;
}
