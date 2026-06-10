using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Auth;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        request.Role = RoleConstants.Customer;
        var result = await authService.RegisterAsync(request, null, ct);
        return Ok(ApiResponse<AuthResponse>.Ok(result, "Register success"));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request, CancellationToken ct)
        => Ok(ApiResponse<AuthResponse>.Ok(await authService.LoginAsync(request, ct), "Login success"));

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Me(CancellationToken ct)
        => Ok(ApiResponse<object>.Ok(await authService.MeAsync(User.GetUserId(), ct)));

    [HttpPut("change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        await authService.ChangePasswordAsync(User.GetUserId(), request, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Password changed"));
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken ct)
        => Ok(ApiResponse<AuthResponse>.Ok(await authService.RefreshTokenAsync(request.RefreshToken, ct), "Refresh success"));

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Logout([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        await authService.LogoutAsync(request.RefreshToken, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Logout success"));
    }
}
