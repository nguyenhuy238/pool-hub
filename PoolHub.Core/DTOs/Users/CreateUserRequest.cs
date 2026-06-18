using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Users;

public class CreateUserRequest
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

    public string? Role { get; set; }
}
