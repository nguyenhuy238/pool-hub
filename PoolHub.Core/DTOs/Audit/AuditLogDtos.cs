using PoolHub.Core.DTOs.Common;

namespace PoolHub.Core.DTOs.Audit;

public class AuditLogQueryRequest : PaginationRequest
{
    public long? ActorUserId { get; set; }
    public string? Action { get; set; }
    public string? EntityName { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class AuditLogDto
{
    public long AuditLogId { get; set; }
    public long? ActorUserId { get; set; }
    public string? ActorName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public long? EntityId { get; set; }
    public Guid? EntityPublicId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
