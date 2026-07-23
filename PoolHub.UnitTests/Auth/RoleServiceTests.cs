using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Roles;
using PoolHub.Core.Entities;
using PoolHub.Infrastructure.Data;
using PoolHub.Services.Audit;
using PoolHub.Services.Roles;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Exceptions;

namespace PoolHub.UnitTests.Auth;

public class RoleServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenNameIsRetired_ThrowsBusinessRuleException()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CreateAsync(new CreateRoleRequest { Name = " cashier " }, 1, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_WhenNameDiffersOnlyByCase_ThrowsConflictException()
    {
        using var db = CreateDb();
        db.Roles.Add(new Role { Name = RoleConstants.Staff, IsSystem = true, IsActive = true });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(new CreateRoleRequest { Name = "staff" }, 1, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_WhenRoleIsSystem_ThrowsBusinessRuleException()
    {
        using var db = CreateDb();
        db.Roles.Add(new Role { RoleId = 1, Name = RoleConstants.Manager, IsSystem = true, IsActive = true });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.DeleteAsync(1, 1, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_WhenRoleIsAssigned_ThrowsConflictException()
    {
        using var db = CreateDb();
        db.Users.Add(new User { UserId = 1, FullName = "User", Email = "user@poolhub.test", PasswordHash = "hash" });
        db.Roles.Add(new Role { RoleId = 1, Name = "Shift Lead", IsSystem = false, IsActive = true });
        db.UserRoles.Add(new UserRole { UserId = 1, RoleId = 1 });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await Assert.ThrowsAsync<ConflictException>(() => service.DeleteAsync(1, 1, CancellationToken.None));
    }

    [Fact]
    public async Task SetPermissionsAsync_WhenPermissionIsInvalid_ThrowsValidationException()
    {
        using var db = CreateDb();
        db.Roles.Add(new Role { RoleId = 1, Name = "Shift Lead", IsSystem = false, IsActive = true });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await Assert.ThrowsAsync<PoolHub.Shared.Exceptions.ValidationException>(() =>
            service.SetPermissionsAsync(1, new UpdateRolePermissionsRequest { PermissionIds = [404] }, 1, CancellationToken.None));
    }

    [Fact]
    public async Task SetPermissionsAsync_WhenAdminWouldLoseConfiguredPermission_ThrowsBusinessRuleException()
    {
        using var db = CreateDb();
        SeedPermissions(db);
        db.Roles.Add(new Role { RoleId = 1, Name = RoleConstants.Admin, IsSystem = true, IsActive = true });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.SetPermissionsAsync(1, new UpdateRolePermissionsRequest { PermissionIds = [1] }, 1, CancellationToken.None));
    }

    [Fact]
    public async Task SetPermissionsAsync_ReplacesPermissionsAndReturnsFinalRole()
    {
        using var db = CreateDb();
        SeedPermissions(db);
        db.Roles.Add(new Role { RoleId = 1, Name = "Shift Lead", IsSystem = false, IsActive = true });
        db.RolePermissions.Add(new RolePermission { RoleId = 1, PermissionId = 1 });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.SetPermissionsAsync(
            1,
            new UpdateRolePermissionsRequest { PermissionIds = [2, 2, 3] },
            1,
            CancellationToken.None);

        Assert.Equal([PermissionConstants.CustomersManage, PermissionConstants.RolesManage], result.PermissionCodes.Order(StringComparer.Ordinal));
        Assert.Equal(2, await db.RolePermissions.CountAsync(x => x.RoleId == 1));
    }

    private static PoolHubDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PoolHubDbContext(options);
    }

    private static RoleService CreateService(PoolHubDbContext db) =>
        new(db, new AuditService(db, new HttpContextAccessor()));

    private static void SeedPermissions(PoolHubDbContext db)
    {
        var id = 1;
        foreach (var code in PermissionConstants.All)
        {
            db.Permissions.Add(new Permission
            {
                PermissionId = id++,
                Code = code,
                Name = code,
                Group = code.Split('.')[0],
                IsActive = true
            });
        }
    }
}
