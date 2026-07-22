namespace PoolHub.Core.Interfaces.Services;

public interface IPosNotificationService
{
    Task NotifyTableUpdateAsync(int tableId, CancellationToken ct = default);
    Task NotifyBookingUpdateAsync(int bookingId, CancellationToken ct = default);
    Task NotifySessionUpdateAsync(int sessionId, CancellationToken ct = default);
    Task NotifyRefreshPosAsync(CancellationToken ct = default);
    Task NotifySessionStartedAsync(int sessionId, int tableId, CancellationToken ct = default) => NotifySessionUpdateAsync(sessionId, ct);
    Task NotifySessionClosedAsync(int sessionId, int? tableId = null, CancellationToken ct = default) => NotifySessionUpdateAsync(sessionId, ct);
    Task NotifySessionTransferredAsync(int sessionId, int fromTableId, int toTableId, CancellationToken ct = default) => NotifySessionUpdateAsync(sessionId, ct);
    Task NotifyOrderUpdatedAsync(int sessionId, int? orderId = null, CancellationToken ct = default) => NotifySessionUpdateAsync(sessionId, ct);
}
