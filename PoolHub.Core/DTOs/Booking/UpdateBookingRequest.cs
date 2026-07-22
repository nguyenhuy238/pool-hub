namespace PoolHub.Core.DTOs.Booking;

public class UpdateBookingRequest
{
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public long? TableId { get; set; }
    public List<long>? TableIds { get; set; }
    public long? TableTypeId { get; set; }
    public int NumberOfGuests { get; set; }
    public string? Note { get; set; }
}
