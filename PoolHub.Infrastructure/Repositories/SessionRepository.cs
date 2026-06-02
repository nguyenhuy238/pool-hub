using Microsoft.EntityFrameworkCore;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Repositories;
using PoolHub.Infrastructure.Data;

namespace PoolHub.Infrastructure.Repositories;

public class SessionRepository(PoolHubDbContext db) : ISessionRepository
{
    public async Task<Session?> GetSessionByIdAsync(int id, CancellationToken ct) => await db.Sessions.FindAsync([id], ct);
    
    public async Task AddSessionAsync(Session session, CancellationToken ct) => await db.Sessions.AddAsync(session, ct);
    
    public async Task AddAssignmentAsync(SessionTableAssignment assignment, CancellationToken ct) => await db.SessionTableAssignments.AddAsync(assignment, ct);
    
    public async Task<SessionTableAssignment?> GetActiveAssignmentAsync(int sessionId, CancellationToken ct)
    {
        return await db.SessionTableAssignments
            .Where(x => x.SessionId == sessionId && x.ReleasedAtUtc == null)
            .OrderByDescending(x => x.AssignedAtUtc)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);
}
