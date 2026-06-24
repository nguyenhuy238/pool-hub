using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PoolHub.Core.DTOs.Auth;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared.Constants;

namespace PoolHub.Services.Auth;

public class TokenService(IOptions<JwtSettings> jwtOptions) : ITokenService
{
    private readonly JwtSettings _jwt = jwtOptions.Value;

    public TokenPair CreateTokenPair(User user, IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions)
    {
        var now = DateTime.UtcNow;
        var accessExpiresAt = now.AddMinutes(_jwt.AccessTokenExpirationMinutes);
        var refreshExpiresAt = now.AddDays(_jwt.RefreshTokenExpirationDays);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Email, user.Email),
            new("fullName", user.FullName),
            new("publicId", user.PublicId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(permissions.Select(permission => new Claim(PermissionConstants.ClaimType, permission)));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = accessExpiresAt,
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SecretKey)),
                SecurityAlgorithms.HmacSha256)
        };

        var handler = new JwtSecurityTokenHandler();
        var accessToken = handler.WriteToken(handler.CreateToken(descriptor));
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        return new TokenPair(accessToken, refreshToken, accessExpiresAt, refreshExpiresAt);
    }

    public string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
