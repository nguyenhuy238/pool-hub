namespace PoolHub.Core.DTOs.Booking;

public class BookingDto
{
    public long BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public long CustomerId { get; set; }
    public long? TableId { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public int Status { get; set; }
}
