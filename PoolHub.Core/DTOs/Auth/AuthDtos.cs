using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Auth;

public class RegisterRequest
{
    [Required, MaxLength(200)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(8)] public string Password { get; set; } = string.Empty;
    [Required] public string Role { get; set; } = string.Empty;
}

public class LoginRequest { [Required, EmailAddress] public string Email { get; set; } = string.Empty; [Required] public string Password { get; set; } = string.Empty; }
public class ChangePasswordRequest { [Required] public string CurrentPassword { get; set; } = string.Empty; [Required, MinLength(8)] public string NewPassword { get; set; } = string.Empty; [Required] public string ConfirmNewPassword { get; set; } = string.Empty; }
public class RefreshTokenRequest { [Required] public string RefreshToken { get; set; } = string.Empty; }
public class AuthResponse { public string AccessToken { get; set; } = string.Empty; public string RefreshToken { get; set; } = string.Empty; public DateTime ExpiresAtUtc { get; set; } public int UserId { get; set; } public string Email { get; set; } = string.Empty; public string FullName { get; set; } = string.Empty; public List<string> Roles { get; set; } = []; }
