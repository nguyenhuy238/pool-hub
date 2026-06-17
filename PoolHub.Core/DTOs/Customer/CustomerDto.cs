namespace PoolHub.Core.DTOs.Customer;

/// <summary>
/// Thông tin đầy đủ của một khách hàng.
/// </summary>
public class CustomerDto
{
    /// <summary>ID khách hàng.</summary>
    public long CustomerId { get; set; }

    /// <summary>Tên đầy đủ.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Số điện thoại.</summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>Email (tùy chọn).</summary>
    public string? Email { get; set; }

    /// <summary>Ghi chú nội bộ.</summary>
    public string? Note { get; set; }

    /// <summary>Trạng thái: true = Active, false = Inactive.</summary>
    public bool Status { get; set; }

    /// <summary>Thời điểm tạo (UTC).</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Tổng số lần đặt bàn.</summary>
    public int TotalBookings { get; set; }
}
