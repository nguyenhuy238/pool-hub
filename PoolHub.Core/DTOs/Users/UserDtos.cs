using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Users;

public class UserDto { public long UserId { get; set; } public Guid PublicId { get; set; } public string FullName { get; set; } = string.Empty; public string Email { get; set; } = string.Empty; public string? PhoneNumber { get; set; } public bool Status { get; set; } public List<string> Roles { get; set; } = []; }
public class CreateUserRequest { [Required] public string FullName { get; set; } = string.Empty; [Required, EmailAddress] public string Email { get; set; } = string.Empty; [Required] public string Password { get; set; } = string.Empty; [Required] public string Role { get; set; } = string.Empty; }
public class UpdateUserRequest { [Required] public string FullName { get; set; } = string.Empty; public string? PhoneNumber { get; set; } public string? AvatarUrl { get; set; } public bool Status { get; set; } }
public class UpdateUserRoleRequest { [Required] public List<string> Roles { get; set; } = []; }
public class UpdateUserStatusRequest { public bool Status { get; set; } }
