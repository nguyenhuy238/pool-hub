namespace PoolHub.Core.DTOs.Booking;

public class BookingDto
{
    public long BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public long? TableId { get; set; }
    public long? TableTypeId { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public int Status { get; set; }
    public int NumberOfGuests { get; set; }
    public string? Note { get; set; }
}
