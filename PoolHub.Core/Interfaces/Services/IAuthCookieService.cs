using Microsoft.AspNetCore.Http;
using PoolHub.Core.DTOs.Auth;

namespace PoolHub.Core.Interfaces.Services;

public interface IAuthCookieService
{
    void CreateAccessTokenCookie(HttpResponse response, string accessToken, DateTime expiresAtUtc);
    void CreateRefreshTokenCookie(HttpResponse response, string refreshToken, DateTime expiresAtUtc);
    void ClearAuthCookies(HttpResponse response);
    bool TryReadAccessTokenFromCookie(HttpRequest request, out string accessToken);
    string? ReadRefreshTokenFromCookie(HttpRequest request);
}
