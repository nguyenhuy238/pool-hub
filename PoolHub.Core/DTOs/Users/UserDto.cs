namespace PoolHub.Core.DTOs.Users;

public class UserDto
{
    public long UserId { get; set; }
    public Guid PublicId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool Status { get; set; }
    public List<string> Roles { get; set; } = [];
}
