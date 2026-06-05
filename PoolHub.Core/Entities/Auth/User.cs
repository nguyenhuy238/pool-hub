namespace PoolHub.Core.Entities;

public class User : BaseEntity
{
    public long UserId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool Status { get; set; } = true;
    public DateTime? LastLoginAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
