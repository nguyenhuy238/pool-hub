namespace PoolHub.Core.DTOs.Session;

public class StartSessionRequest
{
    public long TableId { get; set; }
    public long? BookingId { get; set; }
    public long? CustomerId { get; set; }
}
