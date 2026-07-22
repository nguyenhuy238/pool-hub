using PoolHub.Core.Entities;

namespace PoolHub.Infrastructure.Repositories;

public interface IAuditLogRepository
{
    Task<List<AuditLog>> GetByActorAsync(long actorUserId, CancellationToken ct);
}
