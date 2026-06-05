using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Booking;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.Interfaces;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController(IBookingService bookingService, ICrudService crud) : ControllerBase
{
    /// <summary>
    /// Lấy danh sách lịch đặt bàn có phân trang và lọc.
    /// </summary>
    /// <param name="request">Các tham số lọc và phân trang (ví dụ: status, date, tableId).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Danh sách lịch đặt bàn.</returns>
    [HttpGet] [Authorize] public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] BookingQueryRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await bookingService.GetBookingsAsync(request, ct)));
    
    /// <summary>
    /// Lấy thông tin chi tiết một lịch đặt bàn theo ID.
    /// </summary>
    /// <param name="id">ID của lịch đặt bàn.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Chi tiết lịch đặt bàn.</returns>
    [HttpGet("{id:int}")] [Authorize] public async Task<ActionResult<ApiResponse<object>>> GetById(int id, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await crud.GetBookingAsync(id, ct)));
    
    /// <summary>
    /// Tạo mới một lịch đặt bàn.
    /// </summary>
    /// <remarks>
    /// Request mẫu:
    ///
    ///     POST /api/bookings
    ///     {
    ///        "customerName": "Nguyễn Văn A",
    ///        "phoneNumber": "0987654321",
    ///        "tableId": 1,
    ///        "tableTypeId": 1,
    ///        "startTimeUtc": "2023-12-01T10:00:00Z",
    ///        "endTimeUtc": "2023-12-01T12:00:00Z",
    ///        "numberOfGuests": 4
    ///     }
    ///
    /// Hệ thống sẽ tự động sử dụng CustomerId nếu được cung cấp, hoặc tạo mới/cập nhật thông tin Khách hàng dựa trên số điện thoại (PhoneNumber).
    /// </remarks>
    /// <param name="request">Thông tin lịch đặt bàn.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Lịch đặt bàn vừa được tạo.</returns>
    [HttpPost] [AllowAnonymous] public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] CreateBookingRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await bookingService.CreateAsync(request, ct)));
    
    /// <summary>
    /// Xác nhận một lịch đặt bàn đang ở trạng thái Pending.
    /// </summary>
    /// <param name="id">ID của lịch đặt bàn cần xác nhận.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Lịch đặt bàn đã được cập nhật.</returns>
    [HttpPut("{id:int}/confirm")] 
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + ",Staff")] 
    public async Task<ActionResult<ApiResponse<object>>> Confirm(int id, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await bookingService.ConfirmAsync(id, ct)));
    
    /// <summary>
    /// Hủy một lịch đặt bàn (Pending hoặc Confirmed).
    /// </summary>
    /// <param name="id">ID của lịch đặt bàn cần hủy.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Lịch đặt bàn đã được cập nhật.</returns>
    [HttpPut("{id:int}/cancel")] 
    [Authorize] 
    public async Task<ActionResult<ApiResponse<object>>> Cancel(int id, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await bookingService.CancelAsync(id, ct)));
    
    /// <summary>
    /// Xóa vĩnh viễn một lịch đặt bàn khỏi database (Chỉ dành cho Admin/Manager).
    /// </summary>
    /// <param name="id">ID của lịch đặt bàn.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Thông báo thành công.</returns>
    [HttpDelete("{id:int}")] [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)] public async Task<ActionResult<ApiResponse<object>>> Delete(int id, CancellationToken ct) { await crud.DeleteBookingAsync(id, ct); return Ok(ApiResponse<object>.Ok(new { }, "Deleted")); }
}
