namespace PoolHub.Core.Entities;

public class RefreshToken : BaseEntity
{
    public long RefreshTokenId { get; set; }
    public long UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? CreatedByIp { get; set; }
    public string? RevokedByIp { get; set; }
    public bool IsRevoked { get; set; }
    public Guid FamilyId { get; set; } = Guid.NewGuid();
    public string? ReplacedByTokenHash { get; set; }
}
