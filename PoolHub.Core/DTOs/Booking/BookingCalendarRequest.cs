using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Booking;

/// <summary>
/// Tham số query cho endpoint lịch đặt bàn (calendar view).
/// </summary>
public class BookingCalendarRequest
{
    /// <summary>
    /// Thời điểm bắt đầu khoảng thời gian cần lấy (UTC).
    /// Ví dụ: 2026-06-01T00:00:00Z
    /// </summary>
    [Required]
    public DateTime From { get; set; }

    /// <summary>
    /// Thời điểm kết thúc khoảng thời gian cần lấy (UTC).
    /// Ví dụ: 2026-06-30T17:00:00Z cho end-exclusive của ngày 2026-06-30 tại Việt Nam.
    /// </summary>
    [Required]
    public DateTime To { get; set; }

    /// <summary>Lọc theo ID bàn cụ thể (tùy chọn).</summary>
    public long? TableId { get; set; }

    /// <summary>
    /// Lọc theo trạng thái booking (tùy chọn):
    /// 1 = Pending, 2 = Confirmed, 3 = Cancelled, 4 = Completed.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>Số trang (mặc định: 1).</summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>Kích thước trang (mặc định: 50, tối đa: 200).</summary>
    public int PageSize { get; set; } = 50;
}
