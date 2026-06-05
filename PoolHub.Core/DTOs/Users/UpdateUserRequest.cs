using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Users;

public class UpdateUserRequest
{
    [Required]
    public string FullName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public bool Status { get; set; }
}
