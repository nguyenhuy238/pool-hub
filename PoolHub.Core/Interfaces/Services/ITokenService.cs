using PoolHub.Core.Entities;

namespace PoolHub.Core.Interfaces.Services;

public interface ITokenService
{
    TokenPair CreateTokenPair(User user, IReadOnlyCollection<string> roles);
    string HashToken(string token);
}

public record TokenPair(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAtUtc, DateTime RefreshTokenExpiresAtUtc);
