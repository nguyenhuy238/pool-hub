using Microsoft.EntityFrameworkCore;
using PoolHub.Core.Entities;
using PoolHub.Infrastructure.Data;

namespace PoolHub.Infrastructure.Repositories;

public interface IAuditLogRepository
{
    Task<List<AuditLog>> GetByActorAsync(long actorUserId, CancellationToken ct);
}

public class AuditLogRepository(PoolHubDbContext db) : IAuditLogRepository
{
    public Task<List<AuditLog>> GetByActorAsync(long actorUserId, CancellationToken ct) =>
        db.AuditLogs.Where(x => x.ActorUserId == actorUserId).ToListAsync(ct);
}
