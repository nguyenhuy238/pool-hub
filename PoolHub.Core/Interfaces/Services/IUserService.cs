using PoolHub.Core.DTOs.Users;
using PoolHub.Shared;

namespace PoolHub.Core.Interfaces.Services;

public interface IUserService
{
    Task<PagedResult<UserDto>> GetUsersAsync(UserQueryRequest request, CancellationToken cancellationToken);
    Task<UserDto> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<UserDto> CreateAsync(CreateUserRequest request, long actorUserId, CancellationToken cancellationToken);
    Task<UserDto> UpdateAsync(long id, UpdateUserRequest request, long actorUserId, CancellationToken cancellationToken);
    Task AssignRolesAsync(long id, UpdateUserRoleRequest request, long actorUserId, CancellationToken cancellationToken);
    Task RemoveRoleAsync(long id, long roleId, long actorUserId, CancellationToken cancellationToken);
    Task UpdateStatusAsync(long id, string status, long actorUserId, CancellationToken cancellationToken);
}
