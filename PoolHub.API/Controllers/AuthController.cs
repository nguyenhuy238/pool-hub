using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Auth;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Extensions;
using Microsoft.AspNetCore.RateLimiting;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register(
        [FromBody] RegisterRequest request, CancellationToken ct)
    {
        // Public registration always receives the safe Customer role in the service.
        request.RoleIds = [];
        var result = await authService.RegisterAsync(request, null, ct);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<AuthResponse>.Ok(result, "Register successfully"));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(
        [FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);
        if (result is null)
        {
            return Unauthorized(ApiResponse<AuthResponse>.Fail(
                "Invalid email or password.",
                ["Invalid email or password."]));
        }

        return Ok(ApiResponse<AuthResponse>.Ok(result, "Login successfully"));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Me(CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await authService.MeAsync(User.GetUserId(), ct)));

    [HttpPut("change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(
        [FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        await authService.ChangePasswordAsync(User.GetUserId(), request, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Password changed successfully"));
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RefreshToken(
        [FromBody] RefreshTokenRequest request, CancellationToken ct) =>
        Ok(ApiResponse<AuthResponse>.Ok(
            await authService.RefreshTokenAsync(request.RefreshToken, ct),
            "Refresh token rotated successfully"));

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Logout(
        [FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        await authService.LogoutAsync(User.GetUserId(), request.RefreshToken, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Logout successfully"));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("PasswordRecovery")]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword(
        [FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        await authService.ForgotPasswordAsync(request, ct);
        return Ok(ApiResponse<object>.Ok(new { },
            "If the email exists, a reset password instruction has been sent."));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("PasswordRecovery")]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(
        [FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        await authService.ResetPasswordAsync(request, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Password reset successfully"));
    }
}
