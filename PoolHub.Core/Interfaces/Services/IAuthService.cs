using PoolHub.Core.DTOs.Auth;
using PoolHub.Core.DTOs.Users;

namespace PoolHub.Core.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, int? currentUserId, CancellationToken cancellationToken);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<UserDto> MeAsync(int userId, CancellationToken cancellationToken);
    Task ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken);
    Task<AuthResponse> RefreshTokenAsync(string token, CancellationToken cancellationToken);
    Task LogoutAsync(string token, CancellationToken cancellationToken);
}

public interface IUserService
{
    Task<PoolHub.Shared.PagedResult<UserDto>> GetUsersAsync(PoolHub.Core.DTOs.Common.PaginationRequest request, CancellationToken cancellationToken);
    Task<UserDto> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);
    Task<UserDto> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken);
    Task UpdateRolesAsync(int id, UpdateUserRoleRequest request, CancellationToken cancellationToken);
    Task UpdateStatusAsync(int id, bool status, CancellationToken cancellationToken);
    Task<List<string>> GetRolesAsync(CancellationToken cancellationToken);
}
