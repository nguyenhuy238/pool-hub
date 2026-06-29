namespace PoolHub.Core.Interfaces.Services;

public interface IPosNotificationService
{
    Task NotifyTableUpdateAsync(int tableId, CancellationToken ct = default);
    Task NotifyBookingUpdateAsync(int bookingId, CancellationToken ct = default);
    Task NotifySessionUpdateAsync(int sessionId, CancellationToken ct = default);
    Task NotifyRefreshPosAsync(CancellationToken ct = default);
}
