using Microsoft.AspNetCore.SignalR;
using PoolHub.API.Hubs;
using PoolHub.Core.Interfaces.Services;

namespace PoolHub.API.Services;

public class PosNotificationService : IPosNotificationService
{
    private readonly IHubContext<PosHub, IPosHubClient> _hubContext;

    public PosNotificationService(IHubContext<PosHub, IPosHubClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyTableUpdateAsync(int tableId, CancellationToken ct = default)
    {
        await _hubContext.Clients.All.ReceiveTableUpdate(tableId);
    }

    public async Task NotifyBookingUpdateAsync(int bookingId, CancellationToken ct = default)
    {
        await _hubContext.Clients.All.ReceiveBookingUpdate(bookingId);
    }

    public async Task NotifySessionUpdateAsync(int sessionId, CancellationToken ct = default)
    {
        await _hubContext.Clients.All.ReceiveSessionUpdate(sessionId);
    }

    public async Task NotifyRefreshPosAsync(CancellationToken ct = default)
    {
        await _hubContext.Clients.All.ReceiveRefreshPos();
    }
}
