using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Users;
using PoolHub.Shared;

namespace PoolHub.Core.Interfaces.Services;

public interface IUserService
{
    Task<PagedResult<UserDto>> GetUsersAsync(PaginationRequest request, CancellationToken cancellationToken);
    Task<UserDto> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);
    Task<UserDto> UpdateAsync(long id, UpdateUserRequest request, CancellationToken cancellationToken);
    Task UpdateRolesAsync(long id, UpdateUserRoleRequest request, CancellationToken cancellationToken);
    Task UpdateStatusAsync(long id, bool status, CancellationToken cancellationToken);
    Task<List<string>> GetRolesAsync(CancellationToken cancellationToken);
}
