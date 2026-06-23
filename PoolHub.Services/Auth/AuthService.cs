using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PoolHub.Core.DTOs.Auth;
using PoolHub.Core.DTOs.Users;
using PoolHub.Core.Entities;
using PoolHub.Core.Enums;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Exceptions;

namespace PoolHub.Services.Auth;

public class AuthService(
    PoolHubDbContext db,
    ITokenService tokenService,
    IAuditService auditService,
    IEmailService emailService,
    IOptions<EmailSettings> emailOptions,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly EmailSettings _emailSettings = emailOptions.Value;

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, long? currentUserId, CancellationToken ct)
    {
        ValidatePasswords(request.Password, request.ConfirmPassword);
        var email = NormalizeEmail(request.Email);
        if (await db.Users.AnyAsync(x => x.Email == email, ct))
            throw new ConflictException("Email already exists.");

        var roleIds = currentUserId.HasValue ? request.RoleIds.Distinct().ToList() : [];
        if (roleIds.Count == 0)
        {
            var defaultRole = await db.Roles.SingleOrDefaultAsync(
                x => x.Name == RoleConstants.Customer && x.IsActive, ct)
                ?? throw new ValidationException("Default registration role is not configured.");
            roleIds.Add(defaultRole.RoleId);
        }

        var validRoles = await db.Roles
            .Where(x => roleIds.Contains(x.RoleId) && x.IsActive)
            .Select(x => x.RoleId)
            .ToListAsync(ct);
        if (validRoles.Count != roleIds.Count)
            throw new ValidationException("One or more roles are invalid.");

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12),
            PhoneNumber = request.PhoneNumber?.Trim(),
            EmailConfirmed = false,
            Status = UserStatus.Active
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        db.UserRoles.AddRange(validRoles.Select(roleId => new UserRole
        {
            UserId = user.UserId,
            RoleId = roleId,
            AssignedByUserId = currentUserId
        }));
        await db.SaveChangesAsync(ct);
        await auditService.LogAsync(currentUserId ?? user.UserId, AuditActions.Register, "User",
            user.UserId, user.PublicId, newValues: new { user.FullName, user.Email, RoleIds = validRoles },
            description: "User registered.", ct: ct);

        return await BuildAuthResponseAsync(user, ct);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            logger.LogWarning("Login failed for normalized email {Email}", email);
            await auditService.LogAsync(null, AuditActions.LoginFailed, "User",
                description: $"Login failed for {email}.", ct: ct);
            return null;
        }

        if (user.Status != UserStatus.Active)
        {
            await auditService.LogAsync(user.UserId, AuditActions.LoginFailed, "User",
                user.UserId, user.PublicId, description: $"Login blocked: account is {user.Status}.", ct: ct);
            throw new LockedException($"Account is {user.Status.ToString().ToLowerInvariant()}.");
        }

        user.LastLoginAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        var response = await BuildAuthResponseAsync(user, ct);
        await db.SaveChangesAsync(ct);
        await auditService.LogAsync(user.UserId, AuditActions.LoginSuccess, "User",
            user.UserId, user.PublicId, description: "Login successful.", ct: ct);
        return response;
    }

    public async Task<UserDto> MeAsync(long userId, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([userId], ct) ?? throw new NotFoundException("User not found.");
        if (user.Status != UserStatus.Active) throw new UnauthorizedException("Account is not active.");
        return await MapUserAsync(user, ct);
    }

    public async Task ChangePasswordAsync(long userId, ChangePasswordRequest request, CancellationToken ct)
    {
        ValidatePasswords(request.NewPassword, request.ConfirmPassword);
        var user = await db.Users.FindAsync([userId], ct) ?? throw new NotFoundException("User not found.");
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedException("Current password is incorrect.");
        if (BCrypt.Net.BCrypt.Verify(request.NewPassword, user.PasswordHash))
            throw new ValidationException("New password must be different from the current password.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, 12);
        user.UpdatedAtUtc = DateTime.UtcNow;
        RevokeAllRefreshTokens(userId);
        await db.SaveChangesAsync(ct);
        await auditService.LogAsync(userId, AuditActions.ChangePassword, "User",
            userId, user.PublicId, description: "Password changed and refresh tokens revoked.", ct: ct);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string token, CancellationToken ct)
    {
        var hash = tokenService.HashToken(token);
        var current = await db.RefreshTokens.FirstOrDefaultAsync(
            x => x.TokenHash == hash && !x.IsRevoked, ct)
            ?? throw new UnauthorizedException("Invalid refresh token.");
        if (current.ExpiresAtUtc <= DateTime.UtcNow)
            throw new UnauthorizedException("Refresh token expired.");

        var user = await db.Users.FindAsync([current.UserId], ct)
            ?? throw new UnauthorizedException("Invalid refresh token.");
        if (user.Status != UserStatus.Active)
            throw new UnauthorizedException("Account is not active.");

        current.IsRevoked = true;
        current.RevokedAtUtc = DateTime.UtcNow;
        current.RevokedByIp = GetIpAddress();
        var response = await BuildAuthResponseAsync(user, ct);
        await db.SaveChangesAsync(ct);
        await auditService.LogAsync(user.UserId, AuditActions.RefreshToken, "RefreshToken",
            current.RefreshTokenId, description: "Refresh token rotated.", ct: ct);
        return response;
    }

    public async Task LogoutAsync(long userId, string token, CancellationToken ct)
    {
        var hash = tokenService.HashToken(token);
        var current = await db.RefreshTokens.FirstOrDefaultAsync(
            x => x.TokenHash == hash && x.UserId == userId && !x.IsRevoked, ct);
        if (current is not null)
        {
            current.IsRevoked = true;
            current.RevokedAtUtc = DateTime.UtcNow;
            current.RevokedByIp = GetIpAddress();
            await db.SaveChangesAsync(ct);
        }

        await auditService.LogAsync(userId, AuditActions.Logout, "RefreshToken",
            current?.RefreshTokenId, description: "Logout completed.", ct: ct);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct)
    {
        emailService.EnsureConfigured();
        var email = NormalizeEmail(request.Email);
        var user = await db.Users.FirstOrDefaultAsync(
            x => x.Email == email && x.Status == UserStatus.Active, ct);
        if (user is not null)
        {
            var now = DateTime.UtcNow;
            var latestRequestAt = await db.PasswordResetTokens
                .Where(x => x.UserId == user.UserId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => (DateTime?)x.CreatedAtUtc)
                .FirstOrDefaultAsync(ct);
            if (latestRequestAt.HasValue && latestRequestAt.Value > now.AddMinutes(-1))
            {
                return;
            }

            var previous = await db.PasswordResetTokens
                .Where(x => x.UserId == user.UserId && x.UsedAtUtc == null && x.ExpiresAtUtc > now)
                .ToListAsync(ct);
            foreach (var item in previous) item.UsedAtUtc = now;

            var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
            db.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = user.UserId,
                TokenHash = tokenService.HashToken(rawToken),
                ExpiresAtUtc = now.AddMinutes(Math.Clamp(_emailSettings.PasswordResetExpirationMinutes, 5, 120)),
                RequestedByIp = GetIpAddress()
            });
            await db.SaveChangesAsync(ct);
            try
            {
                await emailService.SendPasswordResetAsync(user.Email, rawToken, ct);
            }
            catch (ServiceUnavailableException)
            {
                var failedToken = await db.PasswordResetTokens
                    .FirstAsync(x => x.UserId == user.UserId && x.TokenHash == tokenService.HashToken(rawToken), ct);
                failedToken.UsedAtUtc = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
                await auditService.LogAsync(user.UserId, AuditActions.ForgotPasswordEmailFailed, "User",
                    user.UserId, user.PublicId, description: "Password reset email delivery failed.", ct: ct);
                return;
            }
            await auditService.LogAsync(user.UserId, AuditActions.ForgotPassword, "User",
                user.UserId, user.PublicId, description: "Password reset requested.", ct: ct);
        }
        else
        {
            await auditService.LogAsync(null, AuditActions.ForgotPassword, "User",
                description: "Password reset requested for an unknown or inactive email.", ct: ct);
        }
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct)
    {
        ValidatePasswords(request.NewPassword, request.ConfirmPassword);
        var email = NormalizeEmail(request.Email);
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email, ct);
        var hash = tokenService.HashToken(request.Token);
        var resetToken = user is null
            ? null
            : await db.PasswordResetTokens.FirstOrDefaultAsync(
                x => x.UserId == user.UserId && x.TokenHash == hash && x.UsedAtUtc == null, ct);

        if (user is null || resetToken is null || resetToken.ExpiresAtUtc <= DateTime.UtcNow)
        {
            await auditService.LogAsync(user?.UserId, AuditActions.ResetPasswordFailed, "User",
                user?.UserId, user?.PublicId, description: "Invalid or expired password reset token.", ct: ct);
            throw new UnauthorizedException("Invalid or expired password reset token.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, 12);
        user.UpdatedAtUtc = DateTime.UtcNow;
        resetToken.UsedAtUtc = DateTime.UtcNow;
        RevokeAllRefreshTokens(user.UserId);
        await db.SaveChangesAsync(ct);
        await auditService.LogAsync(user.UserId, AuditActions.ResetPasswordSuccess, "User",
            user.UserId, user.PublicId, description: "Password reset successful.", ct: ct);
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(User user, CancellationToken ct)
    {
        var roles = await GetRolesAsync(user.UserId, ct);
        var pair = tokenService.CreateTokenPair(user, roles);
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.UserId,
            TokenHash = tokenService.HashToken(pair.RefreshToken),
            ExpiresAtUtc = pair.RefreshTokenExpiresAtUtc,
            CreatedByIp = GetIpAddress()
        });
        await db.SaveChangesAsync(ct);

        return new AuthResponse
        {
            AccessToken = pair.AccessToken,
            RefreshToken = pair.RefreshToken,
            ExpiresAtUtc = pair.AccessTokenExpiresAtUtc,
            UserId = user.UserId,
            Email = user.Email,
            FullName = user.FullName,
            Roles = roles,
            User = new AuthUserSummary
            {
                UserId = user.UserId,
                PublicId = user.PublicId,
                FullName = user.FullName,
                Email = user.Email,
                Roles = roles
            }
        };
    }

    private async Task<UserDto> MapUserAsync(User user, CancellationToken ct) => new()
    {
        UserId = user.UserId,
        PublicId = user.PublicId,
        FullName = user.FullName,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        AvatarUrl = user.AvatarUrl,
        EmailConfirmed = user.EmailConfirmed,
        Status = user.Status.ToString(),
        LastLoginAtUtc = user.LastLoginAtUtc,
        CreatedAtUtc = user.CreatedAtUtc,
        UpdatedAtUtc = user.UpdatedAtUtc,
        Roles = await GetRolesAsync(user.UserId, ct)
    };

    private Task<List<string>> GetRolesAsync(long userId, CancellationToken ct) =>
        (from ur in db.UserRoles
         join role in db.Roles on ur.RoleId equals role.RoleId
         where ur.UserId == userId && role.IsActive
         select role.Name).ToListAsync(ct);

    private void RevokeAllRefreshTokens(long userId)
    {
        var now = DateTime.UtcNow;
        foreach (var token in db.RefreshTokens.Where(x => x.UserId == userId && !x.IsRevoked))
        {
            token.IsRevoked = true;
            token.RevokedAtUtc = now;
            token.RevokedByIp = GetIpAddress();
        }
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static void ValidatePasswords(string password, string confirmPassword)
    {
        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
            throw new ValidationException("Confirm password mismatch.");
        PasswordPolicy.Validate(password);
    }

    private string? GetIpAddress() =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
