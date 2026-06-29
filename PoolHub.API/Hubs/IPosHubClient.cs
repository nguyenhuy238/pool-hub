namespace PoolHub.API.Hubs;

public interface IPosHubClient
{
    Task ReceiveTableUpdate(int tableId);
    Task ReceiveBookingUpdate(int bookingId);
    Task ReceiveSessionUpdate(int sessionId);
    Task ReceiveRefreshPos();
}
