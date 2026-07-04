using Microsoft.AspNetCore.SignalR;
using PoolHub.API.Hubs;
using PoolHub.Core.Interfaces.Services;

namespace PoolHub.API.Services;

public class PosNotificationService : IPosNotificationService
{
    private readonly IHubContext<PosHub, IPosHubClient> _hubContext;
    private readonly IHubContext<OperationHub, IOperationHubClient> _operationHubContext;

    public PosNotificationService(
        IHubContext<PosHub, IPosHubClient> hubContext,
        IHubContext<OperationHub, IOperationHubClient> operationHubContext)
    {
        _hubContext = hubContext;
        _operationHubContext = operationHubContext;
    }

    public async Task NotifyTableUpdateAsync(int tableId, CancellationToken ct = default)
    {
        await _hubContext.Clients.All.ReceiveTableUpdate(tableId);
        await _operationHubContext.Clients.All.TableStatusChanged(new OperationEventPayload
        {
            EventType = "TableStatusChanged",
            TableId = tableId
        });
    }

    public async Task NotifyBookingUpdateAsync(int bookingId, CancellationToken ct = default)
    {
        await _hubContext.Clients.All.ReceiveBookingUpdate(bookingId);
        await _operationHubContext.Clients.All.BookingUpdated(new OperationEventPayload
        {
            EventType = "BookingUpdated",
            BookingId = bookingId
        });
    }

    public async Task NotifySessionUpdateAsync(int sessionId, CancellationToken ct = default)
    {
        await _hubContext.Clients.All.ReceiveSessionUpdate(sessionId);
        await _operationHubContext.Clients.All.SessionUpdated(new OperationEventPayload
        {
            EventType = "SessionUpdated",
            SessionId = sessionId
        });
    }

    public async Task NotifySessionStartedAsync(int sessionId, int tableId, CancellationToken ct = default)
    {
        await NotifyTableUpdateAsync(tableId, ct);
        var payload = new OperationEventPayload
        {
            EventType = "SessionStarted",
            SessionId = sessionId,
            TableId = tableId
        };
        await _hubContext.Clients.All.ReceiveSessionUpdate(sessionId);
        await _operationHubContext.Clients.All.SessionStarted(payload);
        await _operationHubContext.Clients.All.SessionUpdated(payload);
    }

    public async Task NotifySessionClosedAsync(int sessionId, int? tableId = null, CancellationToken ct = default)
    {
        if (tableId.HasValue)
        {
            await NotifyTableUpdateAsync(tableId.Value, ct);
        }

        var payload = new OperationEventPayload
        {
            EventType = "SessionClosed",
            SessionId = sessionId,
            TableId = tableId
        };
        await _hubContext.Clients.All.ReceiveSessionUpdate(sessionId);
        await _operationHubContext.Clients.All.SessionClosed(payload);
        await _operationHubContext.Clients.All.SessionUpdated(payload);
    }

    public async Task NotifySessionTransferredAsync(int sessionId, int fromTableId, int toTableId, CancellationToken ct = default)
    {
        await NotifyTableUpdateAsync(fromTableId, ct);
        await NotifyTableUpdateAsync(toTableId, ct);

        var payload = new OperationEventPayload
        {
            EventType = "SessionTransferred",
            SessionId = sessionId,
            TableId = toTableId
        };
        await _hubContext.Clients.All.ReceiveSessionUpdate(sessionId);
        await _operationHubContext.Clients.All.SessionTransferred(payload);
        await _operationHubContext.Clients.All.SessionUpdated(payload);
    }

    public async Task NotifyOrderUpdatedAsync(int sessionId, int? orderId = null, CancellationToken ct = default)
    {
        var payload = new OperationEventPayload
        {
            EventType = "OrderUpdated",
            SessionId = sessionId
        };
        await _hubContext.Clients.All.ReceiveSessionUpdate(sessionId);
        await _operationHubContext.Clients.All.OrderUpdated(payload);
        await _operationHubContext.Clients.All.SessionUpdated(payload);
    }

    public async Task NotifyRefreshPosAsync(CancellationToken ct = default)
    {
        await _hubContext.Clients.All.ReceiveRefreshPos();
        await _operationHubContext.Clients.All.SessionUpdated(new OperationEventPayload
        {
            EventType = "OperationRefresh"
        });
    }
}
