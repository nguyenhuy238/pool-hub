using PoolHub.Core.DTOs.Audit;
using PoolHub.Shared;

namespace PoolHub.Core.Interfaces.Services;

public interface IAuditService
{
    Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(AuditLogQueryRequest request, CancellationToken ct);
    Task<AuditLogDto> GetAuditLogAsync(long id, CancellationToken ct);
    Task LogAsync(
        long? actorUserId,
        string action,
        string entityName,
        long? entityId = null,
        Guid? entityPublicId = null,
        object? oldValues = null,
        object? newValues = null,
        string? description = null,
        CancellationToken ct = default);
}
