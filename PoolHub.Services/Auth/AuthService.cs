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
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Exceptions;
using PoolHub.Shared.Time;
using CustomerEntity = PoolHub.Core.Entities.Customer;

namespace PoolHub.Services.Auth;

public class AuthService(
    PoolHubDbContext db,
    ITokenService tokenService,
    IRefreshTokenStore refreshTokenStore,
    IAuditService auditService,
    IEmailService emailService,
    IOptions<EmailSettings> emailOptions,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuthService> logger,
    IClock? clock = null) : IAuthService
{
    private readonly EmailSettings _emailSettings = emailOptions.Value;
    private readonly IClock _clock = clock ?? SystemClock.Instance;

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, long? currentUserId, CancellationToken ct)
    {
        ValidatePasswords(request.Password, request.ConfirmPassword);
        var email = NormalizeEmail(request.Email);
        if (await db.Users.AnyAsync(x => x.Email == email, ct))
            throw new ConflictException("Email already exists.");

        var normalizedPhone = PhoneNumberNormalizer.Normalize(request.PhoneNumber);
        CustomerEntity? existingCustomer = null;
        if (!currentUserId.HasValue)
        {
            if (string.IsNullOrWhiteSpace(normalizedPhone))
                throw new ValidationException("Phone number is required for a customer account.");

            var customerByEmail = await db.Customers
                .FirstOrDefaultAsync(
                    x => x.Email != null && x.Email.ToLower() == email,
                    ct);
            var customerByPhone = await db.Customers
                .FirstOrDefaultAsync(x => x.PhoneNumber == normalizedPhone, ct);
            if (customerByEmail is not null && customerByPhone is not null &&
                customerByEmail.CustomerId != customerByPhone.CustomerId)
            {
                throw new ConflictException("The email and phone number belong to different customer profiles.");
            }

            existingCustomer = customerByEmail ?? customerByPhone;
            if (existingCustomer?.UserId is not null)
                throw new ConflictException("This customer profile is already linked to an account.");
            if (existingCustomer is not null && !existingCustomer.Status)
                throw new ConflictException("This customer profile is inactive.");
            if (existingCustomer is not null &&
                !string.IsNullOrWhiteSpace(existingCustomer.Email) &&
                !string.Equals(existingCustomer.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                throw new ConflictException("The phone number belongs to a different customer email.");
            }
            if (existingCustomer is not null &&
                !string.IsNullOrWhiteSpace(existingCustomer.PhoneNumber) &&
                !string.Equals(
                    PhoneNumberNormalizer.Normalize(existingCustomer.PhoneNumber),
                    normalizedPhone,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ConflictException("The email belongs to a different customer phone number.");
            }
        }

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

        // User, role assignment and the customer profile must be created as one
        // unit. Without a transaction a duplicate phone/customer race can leave
        // an orphaned user account after the profile insert fails.
        User user;
        await using (var transaction = await BeginTransactionIfSupportedAsync(ct))
        {
            user = new User
            {
                FullName = request.FullName.Trim(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12),
                PhoneNumber = string.IsNullOrWhiteSpace(normalizedPhone) ? null : normalizedPhone,
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

            if (!currentUserId.HasValue)
            {
                var customer = existingCustomer ?? new CustomerEntity
                {
                    FullName = user.FullName,
                    PhoneNumber = normalizedPhone,
                    Email = user.Email,
                    Status = true
                };
                customer.UserId = user.UserId;
                // Do not reactivate a profile that an administrator disabled.
                // Inactive profiles are rejected above before the account is
                // created; active profiles retain their current status.
                if (existingCustomer is null)
                {
                    customer.FullName = user.FullName;
                    customer.PhoneNumber = normalizedPhone;
                    customer.Email = user.Email;
                    customer.Status = true;
                    db.Customers.Add(customer);
                }
                await db.SaveChangesAsync(ct);
            }

            if (transaction is not null)
            {
                await transaction.CommitAsync(ct);
            }
        }

        await auditService.LogAsync(currentUserId ?? user.UserId, AuditActions.Register, "User",
            user.UserId, user.PublicId, newValues: new { user.FullName, user.Email, RoleIds = validRoles },
            description: "User registered.", ct: ct);

        return await BuildAuthResponseAsync(user, ct);
    }

    public Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct) =>
        LoginAsync(request, ct, null);

    public async Task<AuthResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken ct,
        IReadOnlyCollection<string>? allowedRoles = null)
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

        var roles = await GetRolesAsync(user.UserId, ct);
        if (allowedRoles is not null && !roles.Any(role => allowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase)))
        {
            await auditService.LogAsync(user.UserId, AuditActions.LoginFailed, "User",
                user.UserId, user.PublicId,
                description: "Login blocked because the account is not assigned to this portal.",
                ct: ct);
            throw new ForbiddenException("This account is not allowed to sign in to this portal.");
        }

        user.LastLoginAtUtc = _clock.UtcNow;
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
        user.UpdatedAtUtc = _clock.UtcNow;
        RevokeAllRefreshTokens(userId);
        await db.SaveChangesAsync(ct);
        await auditService.LogAsync(userId, AuditActions.ChangePassword, "User",
            userId, user.PublicId, description: "Password changed and refresh tokens revoked.", ct: ct);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string token, CancellationToken ct)
    {
        var validation = await refreshTokenStore.ValidateRefreshTokenAsync(token, ct);
        if (validation.IsReuseDetected && validation.Record is not null)
        {
            await refreshTokenStore.RevokeFamilyAsync(validation.Record.FamilyId, GetIpAddress(), ct);
            await auditService.LogAsync(validation.Record.UserId, "AUTH_REFRESH_TOKEN_REUSE", "RefreshToken",
                description: "Refresh token reuse detected; token family revoked.", ct: ct);
            throw new UnauthorizedException("Refresh token reuse detected.");
        }

        if (!validation.IsValid || validation.Record is null)
            
            
            throw new UnauthorizedException("Invalid refresh token.");

        var user = await db.Users.FindAsync([validation.Record.UserId], ct)
            ?? throw new UnauthorizedException("Invalid refresh token.");
        if (user.Status != UserStatus.Active)
            throw new UnauthorizedException("Account is not active.");

        var response = await BuildAuthResponseAsync(user, ct, Guid.ParseExact(validation.Record.FamilyId, "N"), validation.Record);
        await db.SaveChangesAsync(ct);
        await auditService.LogAsync(user.UserId, AuditActions.RefreshToken, "RefreshToken",
            description: "Refresh token rotated.", ct: ct);
        return response;
    }

    public async Task LogoutAsync(long? userId, string token, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(token))
            await refreshTokenStore.RevokeRefreshTokenAsync(token, GetIpAddress(), ct);

        await auditService.LogAsync(userId, AuditActions.Logout, "RefreshToken",
            description: "Logout completed.", ct: ct);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct)
    {
        emailService.EnsureConfigured();
        var email = NormalizeEmail(request.Email);
        var user = await db.Users.FirstOrDefaultAsync(
            x => x.Email == email && x.Status == UserStatus.Active, ct);
        if (user is not null)
        {
            var now = _clock.UtcNow;
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

            string otp;
            string otpHash;
            do
            {
                otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
                otpHash = tokenService.HashToken($"{user.UserId}:{otp}");
            } while (await db.PasswordResetTokens.AnyAsync(x => x.TokenHash == otpHash, ct));

            var expirationMinutes = Math.Clamp(_emailSettings.PasswordResetExpirationMinutes, 5, 120);
            db.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = user.UserId,
                TokenHash = otpHash,
                ExpiresAtUtc = now.AddMinutes(expirationMinutes),
                RequestedByIp = GetIpAddress()
            });
            await db.SaveChangesAsync(ct);
            try
            {
                await emailService.SendPasswordResetOtpAsync(user.Email, otp, expirationMinutes, ct);
            }
            catch (ServiceUnavailableException)
            {
                var failedToken = await db.PasswordResetTokens
                    .FirstAsync(x => x.UserId == user.UserId && x.TokenHash == otpHash, ct);
                failedToken.UsedAtUtc = _clock.UtcNow;
                await db.SaveChangesAsync(ct);
                await auditService.LogAsync(user.UserId, AuditActions.ForgotPasswordEmailFailed, "User",
                    user.UserId, user.PublicId, description: "Password reset email delivery failed.", ct: ct);
                throw;
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
        var hash = user is null
            ? string.Empty
            : tokenService.HashToken($"{user.UserId}:{request.Otp}");
        var resetToken = user is null
            ? null
            : await db.PasswordResetTokens.FirstOrDefaultAsync(
                x => x.UserId == user.UserId && x.TokenHash == hash && x.UsedAtUtc == null, ct);

        var now = _clock.UtcNow;
        if (user is null || resetToken is null || resetToken.ExpiresAtUtc <= now)
        {
            await auditService.LogAsync(user?.UserId, AuditActions.ResetPasswordFailed, "User",
                user?.UserId, user?.PublicId, description: "Invalid or expired password reset token.", ct: ct);
            throw new UnauthorizedException("Invalid or expired password reset token.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, 12);
        user.UpdatedAtUtc = now;
        resetToken.UsedAtUtc = now;
        RevokeAllRefreshTokens(user.UserId);
        await db.SaveChangesAsync(ct);
        await auditService.LogAsync(user.UserId, AuditActions.ResetPasswordSuccess, "User",
            user.UserId, user.PublicId, description: "Password reset successful.", ct: ct);
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(
        User user,
        CancellationToken ct,
        Guid? familyId = null,
        RefreshTokenRecord? currentRefreshToken = null)
    {
        var roles = await GetRolesAsync(user.UserId, ct);
        var permissions = await GetPermissionsAsync(user.UserId, ct);
        var pair = tokenService.CreateTokenPair(user, roles, permissions);
        var issuedRefreshToken = currentRefreshToken is null
            ? await refreshTokenStore.IssueRefreshTokenAsync(user.UserId, familyId, GetIpAddress(), GetUserAgent(), ct)
            : await refreshTokenStore.RotateRefreshTokenAsync(currentRefreshToken, GetIpAddress(), GetUserAgent(), ct);

        return new AuthResponse
        {
            AccessToken = pair.AccessToken,
            RefreshToken = issuedRefreshToken.PlainValue,
            ExpiresAtUtc = pair.AccessTokenExpiresAtUtc,
            RefreshTokenExpiresAtUtc = issuedRefreshToken.Record.ExpiresAtUtc,
            UserId = user.UserId,
            Email = user.Email,
            FullName = user.FullName,
            Roles = roles,
            Permissions = permissions,
            User = new AuthUserSummary
            {
                UserId = user.UserId,
                PublicId = user.PublicId,
                FullName = user.FullName,
                Email = user.Email,
                Roles = roles,
                Permissions = permissions
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
        Roles = await GetRolesAsync(user.UserId, ct),
        Permissions = await GetPermissionsAsync(user.UserId, ct)
    };

    private Task<List<string>> GetRolesAsync(long userId, CancellationToken ct) =>
        (from ur in db.UserRoles
         join role in db.Roles on ur.RoleId equals role.RoleId
         where ur.UserId == userId && role.IsActive
         select role.Name).ToListAsync(ct);

    private Task<List<string>> GetPermissionsAsync(long userId, CancellationToken ct) =>
        (from userRole in db.UserRoles
         join rolePermission in db.RolePermissions on userRole.RoleId equals rolePermission.RoleId
         join permission in db.Permissions on rolePermission.PermissionId equals permission.PermissionId
         where userRole.UserId == userId && permission.IsActive
         select permission.Code).Distinct().ToListAsync(ct);

    private void RevokeAllRefreshTokens(long userId)
    {
        var now = _clock.UtcNow;
        foreach (var token in db.RefreshTokens.Where(x => x.UserId == userId && !x.IsRevoked))
        {
            token.IsRevoked = true;
            token.RevokedAtUtc = now;
            token.RevokedByIp = GetIpAddress();
        }
    }

    private async Task RevokeTokenFamilyAsync(long userId, Guid familyId, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var tokens = await db.RefreshTokens
            .Where(x => x.UserId == userId && x.FamilyId == familyId && !x.IsRevoked)
            .ToListAsync(ct);
        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAtUtc = now;
            token.RevokedByIp = GetIpAddress();
        }
        await db.SaveChangesAsync(ct);
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

    private string? GetUserAgent() =>
        httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();

    private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction?> BeginTransactionIfSupportedAsync(
        CancellationToken ct)
    {
        return db.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true
            ? null
            : await db.Database.BeginTransactionAsync(ct);
    }
}
