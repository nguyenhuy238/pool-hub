using PoolHub.Core.DTOs.Common;

namespace PoolHub.Core.DTOs.Booking;

public class BookingQueryRequest : PaginationRequest
{
    public int? Status { get; set; }
    public DateTime? Date { get; set; }
    public long? TableId { get; set; }
}
