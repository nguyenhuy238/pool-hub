using PoolHub.Core.DTOs.Common;

namespace PoolHub.Core.DTOs.AuditLog;

public class AuditLogDto
{
    public int AuditLogId { get; set; }
    public int? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? Metadata { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class AuditLogFilterRequest : PaginationRequest
{
    public string? EntityName { get; set; }
    public int? UserId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}
