using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Booking;

public class BookingDto { public long BookingId { get; set; } public string BookingCode { get; set; } = string.Empty; public long CustomerId { get; set; } public long? TableId { get; set; } public DateTime StartTimeUtc { get; set; } public DateTime EndTimeUtc { get; set; } public int Status { get; set; } }
public class CreateBookingRequest { [Required] public long CustomerId { get; set; } public long? TableId { get; set; } public long? TableTypeId { get; set; } [Required] public DateTime StartTimeUtc { get; set; } [Required] public DateTime EndTimeUtc { get; set; } [Range(1,20)] public int NumberOfGuests { get; set; } }
