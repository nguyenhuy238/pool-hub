using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PoolHub.Core.DTOs.Auth;
using PoolHub.Core.DTOs.Users;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Exceptions;

namespace PoolHub.Services.Services;

public class AuthService(PoolHubDbContext db, IOptions<JwtSettings> jwtOptions) : IAuthService
{
    private readonly JwtSettings _jwt = jwtOptions.Value;

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, long? currentUserId, CancellationToken cancellationToken)
    {
        if (!RoleConstants.All.Contains(request.Role)) throw new ValidationException("Invalid role.");
        var exists = await db.Users.AnyAsync(x => x.Email == request.Email, cancellationToken);
        if (exists) throw new ConflictException("Email already exists.");

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12),
            EmailConfirmed = true,
            Status = true
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        var role = await db.Roles.FirstAsync(x => x.Name == request.Role, cancellationToken);
        db.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = role.RoleId, AssignedByUserId = currentUserId });
        await db.SaveChangesAsync(cancellationToken);

        return await BuildAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == request.Email.ToLower(), cancellationToken) ?? throw new UnauthorizedException("Invalid email or password.");
        if (!user.Status) throw new ForbiddenException("Account is locked.");
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)) throw new UnauthorizedException("Invalid email or password.");

        user.LastLoginAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return await BuildAuthResponseAsync(user, cancellationToken);
    }

    public async Task<UserDto> MeAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([userId], cancellationToken) ?? throw new NotFoundException("User not found.");
        var roles = await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.RoleId where ur.UserId == userId select r.Name).ToListAsync(cancellationToken);
        return new UserDto { UserId = user.UserId, PublicId = user.PublicId, FullName = user.FullName, Email = user.Email, PhoneNumber = user.PhoneNumber, Status = user.Status, Roles = roles };
    }

    public async Task ChangePasswordAsync(long userId, ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (request.NewPassword != request.ConfirmNewPassword) throw new ValidationException("Confirm password mismatch.");
        var user = await db.Users.FindAsync([userId], cancellationToken) ?? throw new NotFoundException("User not found.");
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash)) throw new UnauthorizedException("Current password is incorrect.");
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, 12);
        user.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string token, CancellationToken cancellationToken)
    {
        var hash = ComputeSha256(token);
        var current = await db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash && !x.IsRevoked, cancellationToken) ?? throw new UnauthorizedException("Invalid refresh token.");
        if (current.ExpiresAtUtc <= DateTime.UtcNow) throw new UnauthorizedException("Refresh token expired.");

        current.IsRevoked = true;
        current.RevokedAtUtc = DateTime.UtcNow;

        var user = await db.Users.FindAsync([current.UserId], cancellationToken) ?? throw new NotFoundException("User not found.");
        var response = await BuildAuthResponseAsync(user, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return response;
    }

    public async Task LogoutAsync(string token, CancellationToken cancellationToken)
    {
        var hash = ComputeSha256(token);
        var current = await db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash && !x.IsRevoked, cancellationToken);
        if (current is null) return;
        current.IsRevoked = true;
        current.RevokedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(User user, CancellationToken cancellationToken)
    {
        var roles = await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.RoleId where ur.UserId == user.UserId select r.Name).ToListAsync(cancellationToken);
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwt.SecretKey);
        var expiresAt = DateTime.UtcNow.AddHours(_jwt.ExpirationHours);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("fullName", user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
        };

        var token = tokenHandler.CreateToken(descriptor);
        var accessToken = tokenHandler.WriteToken(token);

        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.UserId,
            TokenHash = ComputeSha256(refreshToken),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedByIp = "127.0.0.1"
        });

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAtUtc = expiresAt,
            UserId = user.UserId,
            Email = user.Email,
            FullName = user.FullName,
            Roles = roles
        };
    }

    private static string ComputeSha256(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }
}
