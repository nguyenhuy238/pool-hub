using PoolHub.Core.Entities;

namespace PoolHub.Infrastructure.Repositories;

public interface ISessionRepository
{
    Task<Session?> GetActiveSessionByTableAsync(long tableId, CancellationToken ct);
}
