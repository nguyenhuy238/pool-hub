using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Session;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared.Exceptions;
using EntitySession = PoolHub.Core.Entities.Session;

namespace PoolHub.Services.Session;

public class SessionService(PoolHubDbContext db) : ISessionService
{
    public async Task<SessionDto> StartAsync(long userId, StartSessionRequest request, CancellationToken ct)
    {
        var session = new EntitySession
        {
            SessionCode = $"SS{DateTime.UtcNow:yyyyMMddHHmmss}",
            BookingId = request.BookingId,
            CustomerId = request.CustomerId,
            OpenedByUserId = userId,
            StartedAtUtc = DateTime.UtcNow,
            Status = 1
        };
        db.Sessions.Add(session);
        await db.SaveChangesAsync(ct);

        db.SessionTableAssignments.Add(new SessionTableAssignment { SessionId = session.SessionId, TableId = request.TableId, StartedAtUtc = DateTime.UtcNow, AssignedByUserId = userId });
        await db.SaveChangesAsync(ct);
        return new SessionDto { SessionId = session.SessionId, SessionCode = session.SessionCode, StartedAtUtc = session.StartedAtUtc, EndedAtUtc = session.EndedAtUtc, Status = session.Status };
    }

    public async Task<SessionDto> CloseAsync(long sessionId, long? closedByUserId, CancellationToken ct)
    {
        var session = await db.Sessions.FindAsync([sessionId], ct) ?? throw new NotFoundException("Session not found.");
        session.Status = 2;
        session.EndedAtUtc = DateTime.UtcNow;
        session.ClosedByUserId = closedByUserId;
        await db.SaveChangesAsync(ct);
        return new SessionDto { SessionId = session.SessionId, SessionCode = session.SessionCode, StartedAtUtc = session.StartedAtUtc, EndedAtUtc = session.EndedAtUtc, Status = session.Status };
    }

    public async Task TransferTableAsync(long sessionId, long newTableId, long? assignedByUserId, CancellationToken ct)
    {
        var current = await db.SessionTableAssignments.Where(x => x.SessionId == sessionId && x.EndedAtUtc == null).OrderByDescending(x => x.StartedAtUtc).FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Active assignment not found.");
        current.EndedAtUtc = DateTime.UtcNow;
        db.SessionTableAssignments.Add(new SessionTableAssignment { SessionId = sessionId, TableId = newTableId, StartedAtUtc = DateTime.UtcNow, AssignedByUserId = assignedByUserId });
        await db.SaveChangesAsync(ct);
    }
}
