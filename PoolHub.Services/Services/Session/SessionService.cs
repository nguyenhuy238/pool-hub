using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Session;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Repositories;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared.Exceptions;

namespace PoolHub.Services.Services.Session;

public class SessionService(ISessionRepository repo) : ISessionService
{
    public async Task<SessionDto> StartAsync(int userId, StartSessionRequest request, CancellationToken ct)
    {
        var session = new PoolHub.Core.Entities.Session { SessionCode = $"SE{DateTime.UtcNow:yyyyMMddHHmmss}", BookingId = request.BookingId, StartedByUserId = userId, StartTimeUtc = DateTime.UtcNow, Status = 1 };
        await repo.AddSessionAsync(session, ct);
        await repo.SaveChangesAsync(ct);
        await repo.AddAssignmentAsync(new SessionTableAssignment { SessionId = session.SessionId, TableId = request.TableId, AssignedAtUtc = DateTime.UtcNow, IsPrimary = true }, ct);
        await repo.SaveChangesAsync(ct);
        return new SessionDto { SessionId = session.SessionId, SessionCode = session.SessionCode, StartTimeUtc = session.StartTimeUtc, EndTimeUtc = session.EndTimeUtc, Status = session.Status };
    }

    public async Task<SessionDto> CloseAsync(int sessionId, CancellationToken ct)
    {
        var session = await repo.GetSessionByIdAsync(sessionId, ct) ?? throw new NotFoundException("Session not found.");
        session.Status = 2;
        session.EndTimeUtc = DateTime.UtcNow;
        await repo.SaveChangesAsync(ct);
        return new SessionDto { SessionId = session.SessionId, SessionCode = session.SessionCode, StartTimeUtc = session.StartTimeUtc, EndTimeUtc = session.EndTimeUtc, Status = session.Status };
    }

    public async Task TransferTableAsync(int sessionId, int newTableId, CancellationToken ct)
    {
        var current = await repo.GetActiveAssignmentAsync(sessionId, ct) ?? throw new NotFoundException("Active assignment not found.");
        current.ReleasedAtUtc = DateTime.UtcNow;
        await repo.AddAssignmentAsync(new SessionTableAssignment { SessionId = sessionId, TableId = newTableId, AssignedAtUtc = DateTime.UtcNow, IsPrimary = true }, ct);
        await repo.SaveChangesAsync(ct);
    }
}
