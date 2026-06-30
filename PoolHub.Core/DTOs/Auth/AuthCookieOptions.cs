namespace PoolHub.Core.DTOs.Auth;

public class AuthCookieOptions
{
    public string AccessTokenCookieName { get; set; } = "poolhub_access_token";
    public string RefreshTokenCookieName { get; set; } = "poolhub_refresh_token";
    public bool UseSecureCookies { get; set; } = true;
    public string SameSite { get; set; } = "Strict";
}
