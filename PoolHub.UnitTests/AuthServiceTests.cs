using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PoolHub.Core.DTOs.Auth;
using PoolHub.Core.Entities;
using PoolHub.Infrastructure.Data;
using PoolHub.Services.Auth;

namespace PoolHub.UnitTests;

public class AuthServiceTests
{
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using var db = new PoolHubDbContext(options);
        db.Roles.Add(new Role { RoleId = 1, Name = "Admin" });
        db.Users.Add(new User { UserId = 1, FullName = "Admin", Email = "admin@poolhub.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123", 12), Status = true });
        db.UserRoles.Add(new UserRole { UserId = 1, RoleId = 1 });
        await db.SaveChangesAsync();

        var jwt = Options.Create(new JwtSettings { SecretKey = "UNIT_TEST_SECRET_KEY_12345678901234567890", Issuer = "PoolHub.API", Audience = "PoolHub.Client", ExpirationHours = 8 });
        var service = new AuthService(db, jwt);

        var result = await service.LoginAsync(new LoginRequest { Email = "admin@poolhub.com", Password = "Admin@123" }, default);

        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.Equal("admin@poolhub.com", result.Email);
    }
}
