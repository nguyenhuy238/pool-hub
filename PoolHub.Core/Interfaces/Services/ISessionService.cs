using PoolHub.Core.DTOs.Session;
using PoolHub.Shared;

namespace PoolHub.Core.Interfaces.Services;

public interface ISessionService
{
    Task<PagedResult<SessionDto>> GetSessionsAsync(SessionQueryRequest request, CancellationToken ct);
    Task<SessionDetailDto> GetSessionByIdAsync(long id, CancellationToken ct);
    Task<SessionDto> StartAsync(long userId, StartSessionRequest request, CancellationToken ct);
    Task<SessionDto> CloseAsync(long sessionId, long? closedByUserId, CancellationToken ct);
    Task<CloseSessionResponse> CloseWithSummaryAsync(long sessionId, long? closedByUserId, CloseSessionRequest request, CancellationToken ct);
    Task TransferTableAsync(long sessionId, long newTableId, long? assignedByUserId, CancellationToken ct);
}
