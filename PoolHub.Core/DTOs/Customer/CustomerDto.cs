using System.ComponentModel.DataAnnotations;

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

/// <summary>
/// Request body để cập nhật thông tin khách hàng.
/// </summary>
public class UpdateCustomerRequest
{
    /// <summary>Tên đầy đủ (bắt buộc).</summary>
    [Required(ErrorMessage = "FullName is required.")]
    [MaxLength(200, ErrorMessage = "FullName cannot exceed 200 characters.")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Email (tùy chọn, phải đúng định dạng).</summary>
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [MaxLength(200)]
    public string? Email { get; set; }

    /// <summary>Ghi chú nội bộ.</summary>
    [MaxLength(500)]
    public string? Note { get; set; }

    /// <summary>Trạng thái: true = Active, false = Inactive.</summary>
    public bool Status { get; set; } = true;
}

/// <summary>
/// Query params cho endpoint lấy danh sách khách hàng.
/// </summary>
public class CustomerQueryRequest
{
    /// <summary>Tìm kiếm theo tên hoặc số điện thoại.</summary>
    public string? Search { get; set; }

    /// <summary>Lọc theo trạng thái (true/false). Để trống = lấy tất cả.</summary>
    public bool? Status { get; set; }

    /// <summary>Số trang (mặc định: 1).</summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>Kích thước trang (mặc định: 20).</summary>
    public int PageSize { get; set; } = 20;
}
