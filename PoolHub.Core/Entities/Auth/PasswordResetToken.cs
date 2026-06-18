namespace PoolHub.Core.Entities;

public class PasswordResetToken : BaseEntity
{
    public long PasswordResetTokenId { get; set; }
    public long UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public string? RequestedByIp { get; set; }
}
