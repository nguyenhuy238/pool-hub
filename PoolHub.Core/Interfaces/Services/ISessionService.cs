using PoolHub.Core.DTOs.Session;
using PoolHub.Shared;

namespace PoolHub.Core.Interfaces.Services;

public interface ISessionService
{
    Task<PagedResult<SessionDto>> GetSessionsAsync(SessionQueryRequest request, CancellationToken ct);
    Task<List<ActiveSessionResponse>> GetActiveSessionsAsync(long? floorId, long? zoneId, long? tableId, CancellationToken ct);
    Task<SessionDetailDto> GetActiveSessionByTableAsync(long tableId, CancellationToken ct);
    Task<SessionDetailDto> GetSessionByIdAsync(long id, CancellationToken ct);
    Task<SessionSummaryResponse> GetSummaryAsync(long sessionId, CancellationToken ct);
    Task<SessionTimeChargesResponse> GetTimeChargesAsync(long sessionId, CancellationToken ct);
    Task<SessionDto> StartAsync(long userId, StartSessionRequest request, CancellationToken ct);
    Task<SessionDto> StartFromBookingAsync(long bookingId, long? tableId, long userId, CancellationToken ct);
    Task<SessionDto> CloseAsync(long sessionId, long? closedByUserId, CancellationToken ct);
    Task<CloseSessionResponse> CloseWithSummaryAsync(long sessionId, long? closedByUserId, CloseSessionRequest request, CancellationToken ct);
    Task<SessionDto> CancelAsync(long sessionId, long? cancelledByUserId, CancelSessionRequest request, CancellationToken ct);
    Task TransferTableAsync(long sessionId, long newTableId, long? assignedByUserId, CancellationToken ct);
}
