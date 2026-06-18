namespace PoolHub.Core.DTOs.Booking;

public class BookingAvailabilityRequest
{
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public long? TableTypeId { get; set; }
}

public class AvailableTableDto
{
    public long TableId { get; set; }
    public string TableCode { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public long TableTypeId { get; set; }
    public string TableTypeName { get; set; } = string.Empty;
    public int Capacity { get; set; }
}
