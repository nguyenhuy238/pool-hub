using PoolHub.Core.Entities;

namespace PoolHub.Core.Interfaces.Repositories;

public interface ISessionRepository
{
    Task<Session?> GetSessionByIdAsync(int id, CancellationToken ct);
    Task AddSessionAsync(Session session, CancellationToken ct);
    Task AddAssignmentAsync(SessionTableAssignment assignment, CancellationToken ct);
    Task<SessionTableAssignment?> GetActiveAssignmentAsync(int sessionId, CancellationToken ct);
    
    Task<int> SaveChangesAsync(CancellationToken ct);
}
