namespace PoolHub.Core.DTOs.Booking;

/// <summary>
/// Một mục trong lịch đặt bàn, dùng để hiển thị trên Calendar View ở frontend.
/// </summary>
public class BookingCalendarItem
{
    /// <summary>ID của booking.</summary>
    public long BookingId { get; set; }

    /// <summary>Mã booking (ví dụ: BK20260615102030).</summary>
    public string BookingCode { get; set; } = string.Empty;

    /// <summary>ID khách hàng.</summary>
    public long CustomerId { get; set; }

    /// <summary>Tên đầy đủ của khách hàng.</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>Số điện thoại khách hàng.</summary>
    public string CustomerPhone { get; set; } = string.Empty;

    /// <summary>ID bàn được đặt (null nếu chỉ đặt theo loại bàn).</summary>
    public long? TableId { get; set; }

    /// <summary>Mã bàn (ví dụ: T01).</summary>
    public string? TableCode { get; set; }

    /// <summary>Tên bàn.</summary>
    public string? TableName { get; set; }

    /// <summary>ID loại bàn.</summary>
    public long? TableTypeId { get; set; }

    /// <summary>Tên loại bàn.</summary>
    public string? TableTypeName { get; set; }

    /// <summary>Thời điểm bắt đầu (UTC).</summary>
    public DateTime StartTimeUtc { get; set; }

    /// <summary>Thời điểm kết thúc (UTC).</summary>
    public DateTime EndTimeUtc { get; set; }

    /// <summary>Số khách.</summary>
    public int NumberOfGuests { get; set; }

    /// <summary>
    /// Trạng thái booking:
    /// 1 = Pending, 2 = Confirmed, 3 = Cancelled, 4 = Completed.
    /// </summary>
    public int Status { get; set; }

    /// <summary>Nhãn trạng thái dạng text.</summary>
    public string StatusLabel => Status switch
    {
        1 => "Pending",
        2 => "Confirmed",
        3 => "Cancelled",
        4 => "Completed",
        5 => "NoShow",
        _ => "Unknown"
    };

    /// <summary>Ghi chú booking.</summary>
    public string? Note { get; set; }
    public bool HasSession { get; set; }

    /// <summary>Thời điểm xác nhận (UTC).</summary>
    public DateTime? ConfirmedAtUtc { get; set; }

    /// <summary>Thời điểm hủy (UTC).</summary>
    public DateTime? CancelledAtUtc { get; set; }
}
