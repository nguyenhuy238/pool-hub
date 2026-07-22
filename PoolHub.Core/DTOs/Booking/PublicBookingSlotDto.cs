namespace PoolHub.Core.DTOs.Booking;

public class PublicBookingSlotDto
{
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public int Status { get; set; }
}
