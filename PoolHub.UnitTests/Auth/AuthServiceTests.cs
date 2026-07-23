using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PoolHub.Core.DTOs.Auth;
using PoolHub.Core.Entities;
using PoolHub.Core.Enums;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Services.Auth;
using Microsoft.AspNetCore.Http;
using PoolHub.Services.Audit;

namespace PoolHub.UnitTests;

public class AuthServiceTests
{
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using var db = new PoolHubDbContext(options);
        db.Roles.Add(new Role { RoleId = 1, Name = "Admin" });
        db.Users.Add(new User { UserId = 1, FullName = "Admin", Email = "admin@poolhub.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123", 12), Status = UserStatus.Active });
        db.UserRoles.Add(new UserRole { UserId = 1, RoleId = 1 });
        await db.SaveChangesAsync();

        var jwt = Options.Create(new JwtSettings { SecretKey = "UNIT_TEST_SECRET_KEY_12345678901234567890", Issuer = "PoolHub.API", Audience = "PoolHub.Client", AccessTokenExpirationMinutes = 480 });
        var accessor = new HttpContextAccessor();
        ITokenService tokenService = new TokenService(jwt);
        IRefreshTokenStore refreshTokenStore = new TestRefreshTokenStore();
        IAuditService auditService = new AuditService(db, accessor);
        IEmailService emailService = new TestEmailService();
        var emailSettings = Options.Create(new EmailSettings
        {
            SmtpHost = "smtp.test.local",
            FromEmail = "noreply@poolhub.test",
            FrontendBaseUrl = "http://localhost:3000"
        });
        var service = new AuthService(db, tokenService, refreshTokenStore, auditService, emailService, emailSettings, accessor, NullLogger<AuthService>.Instance);

        var result = await service.LoginAsync(new LoginRequest { Email = " Admin@PoolHub.com ", Password = "Admin@123" }, default);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.Equal("admin@poolhub.com", result.Email);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsNullInsteadOfThrowing()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new PoolHubDbContext(options);
        db.Users.Add(new User
        {
            UserId = 1,
            FullName = "Admin",
            Email = "admin@poolhub.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123", 12),
            Status = UserStatus.Active
        });
        await db.SaveChangesAsync();

        var jwt = Options.Create(new JwtSettings
        {
            SecretKey = "UNIT_TEST_SECRET_KEY_12345678901234567890",
            Issuer = "PoolHub.API",
            Audience = "PoolHub.Client",
            AccessTokenExpirationMinutes = 480
        });
        var accessor = new HttpContextAccessor();
        var service = new AuthService(
            db,
            new TokenService(jwt),
            new TestRefreshTokenStore(),
            new AuditService(db, accessor),
            new TestEmailService(),
            Options.Create(new EmailSettings
            {
                SmtpHost = "smtp.test.local",
                FromEmail = "noreply@poolhub.test",
                FrontendBaseUrl = "http://localhost:3000"
            }),
            accessor,
            NullLogger<AuthService>.Instance);

        var result = await service.LoginAsync(
            new LoginRequest { Email = "admin@poolhub.com", Password = "wrong-password" },
            default);

        Assert.Null(result);
    }

    private sealed class TestEmailService : IEmailService
    {
        public void EnsureConfigured() { }
        public Task SendPasswordResetAsync(string email, string resetToken, CancellationToken cancellationToken) =>
            Task.CompletedTask;
        public Task SendBookingConfirmedAsync(string email, string customerName, string phoneNumber, string bookingCode, string tableName, DateTime startTimeUtc, DateTime endTimeUtc, int numberOfGuests, CancellationToken ct) =>
            Task.CompletedTask;
        public Task SendBookingCancelledAsync(string email, string customerName, string phoneNumber, string bookingCode, string tableName, DateTime startTimeUtc, DateTime endTimeUtc, int numberOfGuests, string reason, CancellationToken ct) =>
            Task.CompletedTask;
        public Task SendDepositRefundNotificationAsync(string email, string subject, string title, string message, IReadOnlyDictionary<string, string> details, string? actionUrl, string? actionText, CancellationToken ct) =>
            Task.CompletedTask;
    }

    private sealed class TestRefreshTokenStore : IRefreshTokenStore
    {
        public Task<RefreshTokenIssueResult> IssueRefreshTokenAsync(long userId, Guid? familyId, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
        {
            var record = new RefreshTokenRecord
            {
                SessionId = Guid.NewGuid().ToString("N"),
                UserId = userId,
                TokenHash = "test-token-hash",
                FamilyId = (familyId ?? Guid.NewGuid()).ToString("N"),
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                CreatedByIp = ipAddress,
                CreatedByUserAgent = userAgent
            };
            return Task.FromResult(new RefreshTokenIssueResult { PlainValue = $"{record.SessionId}.test-secret", Record = record });
        }

        public Task<RefreshTokenValidationResult> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken) =>
            Task.FromResult(new RefreshTokenValidationResult());

        public Task<RefreshTokenIssueResult> RotateRefreshTokenAsync(RefreshTokenRecord current, string? ipAddress, string? userAgent, CancellationToken cancellationToken) =>
            IssueRefreshTokenAsync(current.UserId, Guid.ParseExact(current.FamilyId, "N"), ipAddress, userAgent, cancellationToken);

        public Task RevokeRefreshTokenAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task RevokeFamilyAsync(string familyId, string? ipAddress, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task RevokeUserAsync(long userId, string? ipAddress, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
