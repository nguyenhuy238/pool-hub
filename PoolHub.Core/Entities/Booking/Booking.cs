namespace PoolHub.Core.Entities;

public class Booking : BaseEntity
{
    public long BookingId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long CustomerId { get; set; }
    public long? TableId { get; set; }
    public long? TableTypeId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public int NumberOfGuests { get; set; }
    public int Status { get; set; }
    public string? Note { get; set; }
    public long? ConfirmedByUserId { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
}
