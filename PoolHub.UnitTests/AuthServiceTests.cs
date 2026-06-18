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
        IAuditService auditService = new AuditService(db, accessor);
        IEmailService emailService = new TestEmailService();
        var emailSettings = Options.Create(new EmailSettings
        {
            SmtpHost = "smtp.test.local",
            FromEmail = "noreply@poolhub.test",
            FrontendBaseUrl = "http://localhost:3000"
        });
        var service = new AuthService(db, tokenService, auditService, emailService, emailSettings, accessor, NullLogger<AuthService>.Instance);

        var result = await service.LoginAsync(new LoginRequest { Email = " Admin@PoolHub.com ", Password = "Admin@123" }, default);

        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.Equal("admin@poolhub.com", result.Email);
    }

    private sealed class TestEmailService : IEmailService
    {
        public void EnsureConfigured() { }
        public Task SendPasswordResetAsync(string email, string resetToken, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
