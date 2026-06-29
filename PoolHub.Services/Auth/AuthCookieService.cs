using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PoolHub.Core.DTOs.Auth;
using PoolHub.Core.Interfaces.Services;

namespace PoolHub.Services.Auth;

public class AuthCookieService : IAuthCookieService
{
    public const string AccessTokenProtectorPurpose = "PoolHub.Auth.AccessTokenCookie.v1";

    private readonly AuthCookieOptions _options;
    private readonly IDataProtector _accessTokenProtector;
    private readonly IHostEnvironment _environment;

    public AuthCookieService(
        IOptions<AuthCookieOptions> options,
        IDataProtectionProvider dataProtectionProvider,
        IHostEnvironment environment)
    {
        _options = options.Value;
        _accessTokenProtector = dataProtectionProvider.CreateProtector(AccessTokenProtectorPurpose);
        _environment = environment;
    }

    public void CreateAccessTokenCookie(HttpResponse response, string accessToken, DateTime expiresAtUtc)
    {
        var protectedAccessToken = _accessTokenProtector.Protect(accessToken);
        response.Cookies.Append(_options.AccessTokenCookieName, protectedAccessToken, BuildCookieOptions(expiresAtUtc, "/"));
    }

    public void CreateRefreshTokenCookie(HttpResponse response, string refreshToken, DateTime expiresAtUtc)
    {
        response.Cookies.Append(_options.RefreshTokenCookieName, refreshToken, BuildCookieOptions(expiresAtUtc, "/api/auth"));
    }

    public void ClearAuthCookies(HttpResponse response)
    {
        response.Cookies.Delete(_options.AccessTokenCookieName, BuildCookieOptions(DateTimeOffset.UnixEpoch.UtcDateTime, "/"));
        response.Cookies.Delete(_options.RefreshTokenCookieName, BuildCookieOptions(DateTimeOffset.UnixEpoch.UtcDateTime, "/api/auth"));
    }

    public bool TryReadAccessTokenFromCookie(HttpRequest request, out string accessToken)
    {
        accessToken = string.Empty;
        if (!request.Cookies.TryGetValue(_options.AccessTokenCookieName, out var protectedAccessToken) ||
            string.IsNullOrWhiteSpace(protectedAccessToken))
        {
            return false;
        }

        try
        {
            accessToken = _accessTokenProtector.Unprotect(protectedAccessToken);
            return !string.IsNullOrWhiteSpace(accessToken);
        }
        catch
        {
            accessToken = string.Empty;
            return false;
        }
    }

    public string? ReadRefreshTokenFromCookie(HttpRequest request) =>
        request.Cookies.TryGetValue(_options.RefreshTokenCookieName, out var refreshToken) &&
        !string.IsNullOrWhiteSpace(refreshToken)
            ? refreshToken
            : null;

    private CookieOptions BuildCookieOptions(DateTime expiresAtUtc, string path)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = _options.UseSecureCookies || _environment.IsProduction(),
            SameSite = ParseSameSite(_options.SameSite),
            Path = path,
            Expires = expiresAtUtc,
            MaxAge = expiresAtUtc > DateTime.UtcNow ? expiresAtUtc - DateTime.UtcNow : TimeSpan.Zero
        };
    }

    private static SameSiteMode ParseSameSite(string value) =>
        Enum.TryParse<SameSiteMode>(value, ignoreCase: true, out var mode) ? mode : SameSiteMode.Strict;
}
