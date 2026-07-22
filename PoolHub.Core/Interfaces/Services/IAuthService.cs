using PoolHub.Core.DTOs.Auth;
using PoolHub.Core.DTOs.Users;

namespace PoolHub.Core.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, long? currentUserId, CancellationToken cancellationToken);
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<UserDto> MeAsync(long userId, CancellationToken cancellationToken);
    Task ChangePasswordAsync(long userId, ChangePasswordRequest request, CancellationToken cancellationToken);
    Task<AuthResponse> RefreshTokenAsync(string token, CancellationToken cancellationToken);
    Task LogoutAsync(long? userId, string token, CancellationToken cancellationToken);
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);
}
