using PoolHub.Core.DTOs.Session;

namespace PoolHub.Core.Interfaces.Services;

public interface ISessionService
{
    Task<SessionDto> StartAsync(int userId, StartSessionRequest request, CancellationToken ct);
    Task<SessionDto> CloseAsync(int sessionId, CancellationToken ct);
    Task TransferTableAsync(int sessionId, int newTableId, CancellationToken ct);
}
