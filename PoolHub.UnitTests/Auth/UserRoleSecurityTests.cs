using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Auth;
using PoolHub.Core.DTOs.Users;
using PoolHub.Core.Entities;
using PoolHub.Core.Enums;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Services.Audit;
using PoolHub.Services.Users;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Exceptions;

namespace PoolHub.UnitTests.Auth;

public class UserRoleSecurityTests
{
    [Fact]
    public async Task AssignRolesAsync_WhenRoleChanges_RevokesRefreshTokens()
    {
        using var db = CreateDb();
        SeedUserRolesAndToken(db);
        var refreshStore = new TrackingRefreshTokenStore();
        var service = CreateService(db, refreshStore);

        await service.AssignRolesAsync(1, new UpdateUserRoleRequest { RoleIds = [2] }, 99, CancellationToken.None);

        var token = await db.RefreshTokens.SingleAsync();
        Assert.True(token.IsRevoked);
        Assert.NotNull(token.RevokedAtUtc);
        Assert.Equal(1, refreshStore.RevokedUserIds.Single());
    }

    [Fact]
    public async Task RemoveRoleAsync_WhenRemovingLastActiveAdmin_ThrowsBusinessRuleException()
    {
        using var db = CreateDb();
        db.Roles.Add(new Role { RoleId = 1, Name = RoleConstants.Admin, IsSystem = true, IsActive = true });
        db.Users.Add(new User { UserId = 1, FullName = "Admin", Email = "admin@poolhub.test", PasswordHash = "hash", Status = UserStatus.Active });
        db.UserRoles.Add(new UserRole { UserId = 1, RoleId = 1 });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.RemoveRoleAsync(1, 1, 99, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenLockingLastActiveAdmin_ThrowsBusinessRuleException()
    {
        using var db = CreateDb();
        db.Roles.Add(new Role { RoleId = 1, Name = RoleConstants.Admin, IsSystem = true, IsActive = true });
        db.Users.Add(new User { UserId = 1, FullName = "Admin", Email = "admin@poolhub.test", PasswordHash = "hash", Status = UserStatus.Active });
        db.UserRoles.Add(new UserRole { UserId = 1, RoleId = 1 });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpdateStatusAsync(1, "Locked", 99, CancellationToken.None));
    }

    private static PoolHubDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PoolHubDbContext(options);
    }

    private static UserService CreateService(PoolHubDbContext db, IRefreshTokenStore? refreshTokenStore = null) =>
        new(db, new AuditService(db, new HttpContextAccessor()), refreshTokenStore);

    private static void SeedUserRolesAndToken(PoolHubDbContext db)
    {
        db.Roles.AddRange(
            new Role { RoleId = 1, Name = RoleConstants.Staff, IsSystem = true, IsActive = true },
            new Role { RoleId = 2, Name = RoleConstants.Manager, IsSystem = true, IsActive = true });
        db.Users.Add(new User { UserId = 1, FullName = "Staff", Email = "staff@poolhub.test", PasswordHash = "hash", Status = UserStatus.Active });
        db.UserRoles.Add(new UserRole { UserId = 1, RoleId = 1 });
        db.RefreshTokens.Add(new RefreshToken
        {
            RefreshTokenId = 1,
            UserId = 1,
            TokenHash = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        });
        db.SaveChanges();
    }

    private sealed class TrackingRefreshTokenStore : IRefreshTokenStore
    {
        public List<long> RevokedUserIds { get; } = [];

        public Task<RefreshTokenIssueResult> IssueRefreshTokenAsync(long userId, Guid? familyId, string? ipAddress, string? userAgent, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<RefreshTokenValidationResult> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<RefreshTokenIssueResult> RotateRefreshTokenAsync(RefreshTokenRecord current, string? ipAddress, string? userAgent, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task RevokeRefreshTokenAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task RevokeFamilyAsync(string familyId, string? ipAddress, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task RevokeUserAsync(long userId, string? ipAddress, CancellationToken cancellationToken)
        {
            RevokedUserIds.Add(userId);
            return Task.CompletedTask;
        }
    }
}
