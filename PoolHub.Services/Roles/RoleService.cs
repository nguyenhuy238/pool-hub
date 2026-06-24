using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Roles;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Exceptions;

namespace PoolHub.Services.Roles;

public class RoleService(PoolHubDbContext db, IAuditService auditService) : IRoleService
{
    public async Task<PagedResult<RoleDto>> GetAsync(RoleQueryRequest request, CancellationToken ct)
    {
        request.PageNumber = Math.Max(1, request.PageNumber);
        request.PageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = db.Roles.AsNoTracking().Where(x => x.IsActive);
        var keyword = request.Keyword ?? request.Search;
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            keyword = keyword.Trim();
            query = query.Where(x => x.Name.Contains(keyword) ||
                (x.Description != null && x.Description.Contains(keyword)));
        }

        var total = await query.CountAsync(ct);
        var roles = await query.OrderBy(x => x.RoleId)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);
        var ids = roles.Select(x => x.RoleId).ToList();
        var counts = await db.UserRoles.Where(x => ids.Contains(x.RoleId))
            .GroupBy(x => x.RoleId)
            .Select(x => new { RoleId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, ct);
        var permissionRows = await (from rolePermission in db.RolePermissions
                                    join permission in db.Permissions on rolePermission.PermissionId equals permission.PermissionId
                                    where ids.Contains(rolePermission.RoleId) && permission.IsActive
                                    select new { rolePermission.RoleId, permission.Code }).ToListAsync(ct);
        return new PagedResult<RoleDto>
        {
            Items = roles.Select(x => Map(x, counts.GetValueOrDefault(x.RoleId),
                permissionRows.Where(p => p.RoleId == x.RoleId).Select(p => p.Code).ToList())).ToList(),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalItems = total
        };
    }

    public async Task<RoleDto> GetByIdAsync(long id, CancellationToken ct)
    {
        var role = await db.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.RoleId == id && x.IsActive, ct)
            ?? throw new NotFoundException("Role not found.");
        var count = await db.UserRoles.CountAsync(x => x.RoleId == id, ct);
        var permissions = await (from rolePermission in db.RolePermissions
                                 join permission in db.Permissions on rolePermission.PermissionId equals permission.PermissionId
                                 where rolePermission.RoleId == id && permission.IsActive
                                 select permission.Code).ToListAsync(ct);
        return Map(role, count, permissions);
    }

    public async Task<RoleDto> CreateAsync(CreateRoleRequest request, long actorUserId, CancellationToken ct)
    {
        var name = request.Name.Trim();
        if (await db.Roles.AnyAsync(x => x.Name == name, ct))
            throw new ConflictException("Role name already exists.");
        var role = new Role
        {
            Name = name,
            Description = request.Description?.Trim(),
            IsSystem = request.IsSystem,
            IsActive = true
        };
        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);
        await auditService.LogAsync(actorUserId, AuditActions.RoleCreated, "Role",
            role.RoleId, newValues: new { role.Name, role.Description, role.IsSystem },
            description: "Role created.", ct: ct);
        return Map(role, 0, []);
    }

    public async Task<RoleDto> UpdateAsync(long id, UpdateRoleRequest request, long actorUserId, CancellationToken ct)
    {
        var role = await db.Roles.FirstOrDefaultAsync(x => x.RoleId == id && x.IsActive, ct)
            ?? throw new NotFoundException("Role not found.");
        var name = request.Name.Trim();
        if (role.IsSystem && !string.Equals(role.Name, name, StringComparison.Ordinal))
            throw new BusinessRuleException("System role names cannot be changed.");
        if (!string.Equals(role.Name, name, StringComparison.OrdinalIgnoreCase) &&
            await db.Roles.AnyAsync(x => x.RoleId != id && x.Name == name, ct))
            throw new ConflictException("Role name already exists.");

        var oldValues = new { role.Name, role.Description };
        role.Name = name;
        role.Description = request.Description?.Trim();
        role.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await auditService.LogAsync(actorUserId, AuditActions.RoleUpdated, "Role",
            role.RoleId, oldValues: oldValues, newValues: new { role.Name, role.Description },
            description: "Role updated.", ct: ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task DeleteAsync(long id, long actorUserId, CancellationToken ct)
    {
        var role = await db.Roles.FirstOrDefaultAsync(x => x.RoleId == id && x.IsActive, ct)
            ?? throw new NotFoundException("Role not found.");
        if (role.IsSystem) throw new BusinessRuleException("System roles cannot be deleted.");
        if (await db.UserRoles.AnyAsync(x => x.RoleId == id, ct))
            throw new ConflictException("Role is assigned to one or more users.");

        role.IsActive = false;
        role.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await auditService.LogAsync(actorUserId, AuditActions.RoleDeleted, "Role",
            role.RoleId, oldValues: new { role.Name, role.Description },
            description: "Role soft-deleted.", ct: ct);
    }

    public Task<List<PermissionDto>> GetPermissionsAsync(CancellationToken ct) =>
        db.Permissions.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Group).ThenBy(x => x.Code)
            .Select(x => new PermissionDto
            {
                PermissionId = x.PermissionId,
                Code = x.Code,
                Name = x.Name,
                Group = x.Group,
                Description = x.Description
            }).ToListAsync(ct);

    public async Task SetPermissionsAsync(long roleId, UpdateRolePermissionsRequest request, long actorUserId, CancellationToken ct)
    {
        var role = await db.Roles.FirstOrDefaultAsync(x => x.RoleId == roleId && x.IsActive, ct)
            ?? throw new NotFoundException("Role not found.");
        var permissionIds = request.PermissionIds.Distinct().ToList();
        var validCount = await db.Permissions.CountAsync(x => permissionIds.Contains(x.PermissionId) && x.IsActive, ct);
        if (validCount != permissionIds.Count) throw new ValidationException("One or more permissions are invalid.");

        var existing = await db.RolePermissions.Where(x => x.RoleId == roleId).ToListAsync(ct);
        var oldIds = existing.Select(x => x.PermissionId).ToList();
        db.RolePermissions.RemoveRange(existing);
        db.RolePermissions.AddRange(permissionIds.Select(permissionId => new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId,
            AssignedByUserId = actorUserId
        }));
        role.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await auditService.LogAsync(actorUserId, "ROLE_PERMISSIONS_UPDATED", nameof(Role), roleId,
            oldValues: new { PermissionIds = oldIds }, newValues: new { PermissionIds = permissionIds },
            description: "Role permissions updated.", ct: ct);
    }

    private static RoleDto Map(Role role, int userCount, List<string> permissionCodes) => new()
    {
        RoleId = role.RoleId,
        Name = role.Name,
        Description = role.Description,
        IsSystem = role.IsSystem,
        IsActive = role.IsActive,
        UserCount = userCount,
        CreatedAtUtc = role.CreatedAtUtc,
        UpdatedAtUtc = role.UpdatedAtUtc
        ,PermissionCodes = permissionCodes
    };
}
