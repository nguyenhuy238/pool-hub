namespace PoolHub.API.Hubs;

public interface IOperationHubClient
{
    Task SessionStarted(OperationEventPayload payload);
    Task SessionUpdated(OperationEventPayload payload);
    Task SessionTransferred(OperationEventPayload payload);
    Task SessionClosed(OperationEventPayload payload);
    Task OrderUpdated(OperationEventPayload payload);
    Task BookingUpdated(OperationEventPayload payload);
    Task TableStatusChanged(OperationEventPayload payload);
}

public sealed class OperationEventPayload
{
    public string EventType { get; set; } = string.Empty;
    public int? SessionId { get; set; }
    public int? BookingId { get; set; }
    public int? TableId { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
