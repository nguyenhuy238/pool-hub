using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Auth;

public class RegisterRequest
{
    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string ConfirmPassword { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    public List<long> RoleIds { get; set; } = [];

    // Kept for compatibility with the existing frontend. Public registration ignores it.
    public string? Role { get; set; }
}
