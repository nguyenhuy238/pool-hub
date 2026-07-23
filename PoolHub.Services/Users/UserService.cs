using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Users;
using PoolHub.Core.Entities;
using PoolHub.Core.Enums;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Services.Auth;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Exceptions;
using PoolHub.Shared.Time;

namespace PoolHub.Services.Users;

public class UserService(PoolHubDbContext db, IAuditService auditService, IRefreshTokenStore? refreshTokenStore = null, IClock? clock = null) : IUserService
{
    private readonly IClock _clock = clock ?? SystemClock.Instance;

    public async Task<PagedResult<UserDto>> GetUsersAsync(UserQueryRequest request, CancellationToken ct)
    {
        NormalizePagination(request);
        var query = db.Users.AsNoTracking().AsQueryable();
        var keyword = request.Keyword ?? request.Search;
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            keyword = keyword.Trim();
            query = query.Where(x => x.FullName.Contains(keyword) || x.Email.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<UserStatus>(request.Status, true, out var status))
                throw new ValidationException("Invalid user status.");
            query = query.Where(x => x.Status == status);
        }

        if (request.RoleId.HasValue)
        {
            var roleId = request.RoleId.Value;
            query = query.Where(x => db.UserRoles.Any(ur => ur.UserId == x.UserId && ur.RoleId == roleId));
        }

        var total = await query.CountAsync(ct);
        var users = await query.OrderBy(x => x.UserId)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);
        var userIds = users.Select(x => x.UserId).ToList();
        var roleRows = await (from ur in db.UserRoles
                              join role in db.Roles on ur.RoleId equals role.RoleId
                              where userIds.Contains(ur.UserId) && role.IsActive
                              select new { ur.UserId, role.Name }).ToListAsync(ct);

        return new PagedResult<UserDto>
        {
            Items = users.Select(user => Map(user,
                roleRows.Where(x => x.UserId == user.UserId).Select(x => x.Name).ToList())).ToList(),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalItems = total
        };
    }

    public async Task<UserDto> GetByIdAsync(long id, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == id, ct)
            ?? throw new NotFoundException("User not found.");
        return Map(user, await GetRoleNamesAsync(id, ct));
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, long actorUserId, CancellationToken ct)
    {
        var confirmPassword = string.IsNullOrEmpty(request.ConfirmPassword) ? request.Password : request.ConfirmPassword;
        if (!string.Equals(request.Password, confirmPassword, StringComparison.Ordinal))
            throw new ValidationException("Confirm password mismatch.");
        PasswordPolicy.Validate(request.Password);

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == email, ct))
            throw new ConflictException("Email already exists.");

        var roleIds = request.RoleIds.Distinct().ToList();
        if (roleIds.Count == 0 && !string.IsNullOrWhiteSpace(request.Role))
        {
            var roleName = request.Role.Trim().ToLowerInvariant();
            roleIds = await db.Roles.Where(x => x.Name.ToLower() == roleName && x.IsActive)
                .Select(x => x.RoleId).ToListAsync(ct);
        }
        await ValidateRolesAsync(roleIds, ct);

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12),
            PhoneNumber = request.PhoneNumber?.Trim(),
            EmailConfirmed = true,
            Status = UserStatus.Active
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        db.UserRoles.AddRange(roleIds.Select(roleId => new UserRole
        {
            UserId = user.UserId,
            RoleId = roleId,
            AssignedByUserId = actorUserId
        }));
        await db.SaveChangesAsync(ct);
        await auditService.LogAsync(actorUserId, AuditActions.UserCreated, "User",
            user.UserId, user.PublicId, newValues: new { user.FullName, user.Email, RoleIds = roleIds },
            description: "Internal user created.", ct: ct);
        return await GetByIdAsync(user.UserId, ct);
    }

    public async Task<UserDto> UpdateAsync(long id, UpdateUserRequest request, long actorUserId, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([id], ct) ?? throw new NotFoundException("User not found.");
        var oldValues = new { user.FullName, user.PhoneNumber, user.AvatarUrl, user.EmailConfirmed };
        user.FullName = request.FullName.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim();
        user.AvatarUrl = request.AvatarUrl?.Trim();
        user.EmailConfirmed = request.EmailConfirmed;
        user.UpdatedAtUtc = _clock.UtcNow;
        await db.SaveChangesAsync(ct);
        await auditService.LogAsync(actorUserId, AuditActions.UserUpdated, "User",
            user.UserId, user.PublicId, oldValues,
            new { user.FullName, user.PhoneNumber, user.AvatarUrl, user.EmailConfirmed },
            "User profile updated.", ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task AssignRolesAsync(long id, UpdateUserRoleRequest request, long actorUserId, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([id], ct) ?? throw new NotFoundException("User not found.");
        var roleIds = request.RoleIds.Distinct().ToList();
        if (roleIds.Count == 0 && request.Roles.Count > 0)
        {
            var names = request.Roles.Select(x => x.Trim().ToLowerInvariant()).Distinct().ToList();
            roleIds = await db.Roles.Where(x => names.Contains(x.Name.ToLower()) && x.IsActive)
                .Select(x => x.RoleId).ToListAsync(ct);
        }
        await ValidateRolesAsync(roleIds, ct);

        var existing = await db.UserRoles.Where(x => x.UserId == id && roleIds.Contains(x.RoleId))
            .Select(x => x.RoleId).ToListAsync(ct);
        var additions = roleIds.Except(existing).ToList();
        db.UserRoles.AddRange(additions.Select(roleId => new UserRole
        {
            UserId = id,
            RoleId = roleId,
            AssignedByUserId = actorUserId
        }));
        await db.SaveChangesAsync(ct);
        if (additions.Count > 0)
        {
            await RevokeAllRefreshTokensAsync(id, ct);
            await db.SaveChangesAsync(ct);
            await auditService.LogAsync(actorUserId, AuditActions.UserRoleAssigned, "User",
                user.UserId, user.PublicId, newValues: new { RoleIds = additions },
                description: "Roles assigned to user and refresh tokens revoked.", ct: ct);
        }
    }

    public async Task RemoveRoleAsync(long id, long roleId, long actorUserId, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([id], ct) ?? throw new NotFoundException("User not found.");
        var assignment = await db.UserRoles.FirstOrDefaultAsync(
            x => x.UserId == id && x.RoleId == roleId, ct)
            ?? throw new NotFoundException("User role assignment not found.");
        if (await db.UserRoles.CountAsync(x => x.UserId == id, ct) <= 1)
            throw new BusinessRuleException("A user must have at least one role.");

        var role = await db.Roles.FindAsync([roleId], ct) ?? throw new NotFoundException("Role not found.");
        if (role.Name == RoleConstants.Admin)
        {
            var adminRoleId = role.RoleId;
            var activeAdminCount = await db.UserRoles.CountAsync(ur =>
                ur.RoleId == adminRoleId &&
                db.Users.Any(u => u.UserId == ur.UserId && u.Status == UserStatus.Active), ct);
            if (activeAdminCount <= 1)
                throw new BusinessRuleException("The last active administrator cannot lose the Admin role.");
        }

        db.UserRoles.Remove(assignment);
        await RevokeAllRefreshTokensAsync(id, ct);
        await db.SaveChangesAsync(ct);
        await auditService.LogAsync(actorUserId, AuditActions.UserRoleRemoved, "User",
            user.UserId, user.PublicId, oldValues: new { RoleId = roleId, role.Name },
            description: "Role removed from user and refresh tokens revoked.", ct: ct);
    }

    public async Task UpdateStatusAsync(long id, string status, long actorUserId, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([id], ct) ?? throw new NotFoundException("User not found.");
        if (!Enum.TryParse<UserStatus>(status, true, out var parsed))
            throw new ValidationException("Status must be Active, Locked, or Deleted.");
        if (id == actorUserId && parsed != UserStatus.Active)
            throw new BusinessRuleException("An administrator cannot lock or delete their own account.");
        if (parsed != UserStatus.Active && await IsLastActiveAdminAsync(id, ct))
            throw new BusinessRuleException("The last active administrator cannot be locked or deleted.");

        var oldStatus = user.Status;
        user.Status = parsed;
        var now = _clock.UtcNow;
        user.UpdatedAtUtc = now;
        if (parsed != UserStatus.Active)
        {
            await RevokeAllRefreshTokensAsync(id, ct);
        }
        await db.SaveChangesAsync(ct);
        await auditService.LogAsync(actorUserId, AuditActions.UserStatusChanged, "User",
            user.UserId, user.PublicId, new { Status = oldStatus.ToString() },
            new { Status = parsed.ToString() }, "User status changed.", ct);
    }

    private async Task ValidateRolesAsync(List<long> roleIds, CancellationToken ct)
    {
        if (roleIds.Count == 0) throw new ValidationException("At least one role is required.");
        var roles = await db.Roles.Where(x => roleIds.Contains(x.RoleId) && x.IsActive)
            .Select(x => new { x.RoleId, x.Name }).ToListAsync(ct);
        if (roles.Count != roleIds.Count) throw new ValidationException("One or more roles are invalid.");
        if (roles.Any(x => RoleConstants.Retired.Contains(x.Name, StringComparer.OrdinalIgnoreCase)))
            throw new BusinessRuleException("One or more roles have been retired and cannot be assigned.");
    }

    private async Task<bool> IsLastActiveAdminAsync(long userId, CancellationToken ct)
    {
        var adminRoleId = await db.Roles
            .Where(x => x.Name == RoleConstants.Admin && x.IsActive)
            .Select(x => (long?)x.RoleId)
            .FirstOrDefaultAsync(ct);
        if (!adminRoleId.HasValue) return false;
        var userHasAdmin = await db.UserRoles.AnyAsync(x => x.UserId == userId && x.RoleId == adminRoleId.Value, ct);
        if (!userHasAdmin) return false;
        var activeAdminCount = await db.UserRoles.CountAsync(ur =>
            ur.RoleId == adminRoleId.Value &&
            db.Users.Any(u => u.UserId == ur.UserId && u.Status == UserStatus.Active), ct);
        return activeAdminCount <= 1;
    }

    private async Task RevokeAllRefreshTokensAsync(long userId, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        foreach (var token in db.RefreshTokens.Where(x => x.UserId == userId && !x.IsRevoked))
        {
            token.IsRevoked = true;
            token.RevokedAtUtc = now;
        }

        if (refreshTokenStore is not null)
            await refreshTokenStore.RevokeUserAsync(userId, null, ct);
    }

    private Task<List<string>> GetRoleNamesAsync(long userId, CancellationToken ct) =>
        (from ur in db.UserRoles
         join role in db.Roles on ur.RoleId equals role.RoleId
         where ur.UserId == userId && role.IsActive
         select role.Name).ToListAsync(ct);

    private static UserDto Map(User user, List<string> roles) => new()
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
        Roles = roles
    };

    private static void NormalizePagination(UserQueryRequest request)
    {
        request.PageNumber = Math.Max(1, request.PageNumber);
        request.PageSize = Math.Clamp(request.PageSize, 1, 100);
    }
}
