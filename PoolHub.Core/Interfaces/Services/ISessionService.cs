using PoolHub.Core.DTOs.Session;

namespace PoolHub.Core.Interfaces.Services;

public interface ISessionService
{
    Task<SessionDto> StartAsync(long userId, StartSessionRequest request, CancellationToken ct);
    Task<SessionDto> CloseAsync(long sessionId, long? closedByUserId, CancellationToken ct);
    Task TransferTableAsync(long sessionId, long newTableId, long? assignedByUserId, CancellationToken ct);
}
