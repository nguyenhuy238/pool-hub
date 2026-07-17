using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Booking;

public class CreateBookingRequest
{
    [Required]
    public long CustomerId { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public long? TableId { get; set; }
    public List<long>? TableIds { get; set; }
    public long? TableTypeId { get; set; }

    [Required]
    public DateTime StartTimeUtc { get; set; }

    [Required]
    public DateTime EndTimeUtc { get; set; }

    [Range(1, 20)]
    public int NumberOfGuests { get; set; }

    public string? Note { get; set; }
}
