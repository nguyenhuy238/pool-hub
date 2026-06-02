namespace PoolHub.Core.Entities;

public class Customer : BaseEntity
{
    public int CustomerId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Note { get; set; }
    public bool Status { get; set; } = true;
}

public class Booking : BaseEntity
{
    public int BookingId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public int CustomerId { get; set; }
    public int TableId { get; set; }
    public int TableTypeId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public int NumberOfGuests { get; set; }
    public int Status { get; set; }
    public string? Note { get; set; }
    public int? ConfirmedByUserId { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
}
