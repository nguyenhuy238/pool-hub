using PoolHub.Core.DTOs.Roles;
using PoolHub.Shared;

namespace PoolHub.Core.Interfaces.Services;

public interface IRoleService
{
    Task<PagedResult<RoleDto>> GetAsync(RoleQueryRequest request, CancellationToken ct);
    Task<RoleDto> GetByIdAsync(long id, CancellationToken ct);
    Task<RoleDto> CreateAsync(CreateRoleRequest request, long actorUserId, CancellationToken ct);
    Task<RoleDto> UpdateAsync(long id, UpdateRoleRequest request, long actorUserId, CancellationToken ct);
    Task DeleteAsync(long id, long actorUserId, CancellationToken ct);
    Task<List<PermissionDto>> GetPermissionsAsync(CancellationToken ct);
    Task SetPermissionsAsync(long roleId, UpdateRolePermissionsRequest request, long actorUserId, CancellationToken ct);
}
