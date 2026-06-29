namespace PoolHub.Core.DTOs.Auth;

public class AuthTokenOptions
{
    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 7;
}
