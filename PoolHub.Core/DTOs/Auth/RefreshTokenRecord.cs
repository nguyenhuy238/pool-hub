namespace PoolHub.Core.DTOs.Auth;

public class RefreshTokenRecord
{
    public string SessionId { get; set; } = string.Empty;
    public long UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string FamilyId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public string? CreatedByIp { get; set; }
    public string? CreatedByUserAgent { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedBySessionId { get; set; }
}

public class RefreshTokenIssueResult
{
    public string PlainValue { get; set; } = string.Empty;
    public RefreshTokenRecord Record { get; set; } = new();
}

public class RefreshTokenValidationResult
{
    public bool IsValid { get; set; }
    public bool IsReuseDetected { get; set; }
    public RefreshTokenRecord? Record { get; set; }
}
