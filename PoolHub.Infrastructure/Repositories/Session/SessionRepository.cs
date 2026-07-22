using Microsoft.EntityFrameworkCore;
using PoolHub.Core.Entities;
using PoolHub.Infrastructure.Data;

namespace PoolHub.Infrastructure.Repositories;

public class SessionRepository(PoolHubDbContext db) : ISessionRepository
{
    public async Task<Session?> GetActiveSessionByTableAsync(long tableId, CancellationToken ct)
    {
        var assignment = await db.SessionTableAssignments
            .Where(x => x.TableId == tableId && x.EndedAtUtc == null)
            .OrderByDescending(x => x.StartedAtUtc)
            .FirstOrDefaultAsync(ct);

        return assignment is null
            ? null
            : await db.Sessions.FirstOrDefaultAsync(x => x.SessionId == assignment.SessionId && x.Status == 1, ct);
    }
}
