using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Booking;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Session;
using PoolHub.Core.Interfaces;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController(IBookingService bookingService, ISessionService sessionService, ICrudService crud) : ControllerBase
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
    [HttpGet("{id:int}")] [Authorize] public async Task<ActionResult<ApiResponse<object>>> GetById(int id, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await bookingService.GetByIdAsync(id, ct)));

    /// <summary>
    /// Lấy danh sách booking theo khoảng thời gian — dùng cho Calendar View.
    /// </summary>
    /// <remarks>
    /// Endpoint này trả về toàn bộ booking có khoảng thời gian [startTimeUtc, endTimeUtc]
    /// giao với khoảng [from, to] được chỉ định. Kết quả bao gồm:
    /// - Thông tin đầy đủ khách hàng (tên, SĐT)
    /// - Thông tin bàn và loại bàn
    /// - Trạng thái booking kèm label (Pending/Confirmed/Cancelled/Completed)
    ///
    /// **Cách test trên Swagger:**
    /// 1. Nhập token JWT vào "Authorize"
    /// 2. Truyền `from` = ngày bắt đầu (UTC), `to` = ngày kết thúc (UTC)
    /// 3. Optionally lọc theo `tableId` hoặc `status`
    ///
    /// **Ví dụ request:**
    ///
    ///     GET /api/bookings/calendar?from=2026-06-01T00:00:00Z&amp;to=2026-06-30T23:59:59Z&amp;status=2
    ///
    /// **Ví dụ response:**
    ///
    ///     {
    ///       "data": {
    ///         "items": [
    ///           {
    ///             "bookingId": 1,
    ///             "bookingCode": "BK20260615100000",
    ///             "customerName": "Nguyen Van A",
    ///             "customerPhone": "0987654321",
    ///             "tableCode": "T01",
    ///             "tableName": "Ban so 1",
    ///             "startTimeUtc": "2026-06-15T10:00:00Z",
    ///             "endTimeUtc": "2026-06-15T12:00:00Z",
    ///             "status": 2,
    ///             "statusLabel": "Confirmed"
    ///           }
    ///         ],
    ///         "totalCount": 1,
    ///         "pageNumber": 1,
    ///         "pageSize": 50
    ///       }
    ///     }
    /// </remarks>
    /// <param name="request">Query params: from (required), to (required), tableId, status, pageNumber, pageSize.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Danh sach booking trong khoang thoi gian chi dinh.</returns>
    /// <response code="200">Tra ve danh sach booking thanh cong.</response>
    /// <response code="400">from/to khong hop le (vi du: from > to).</response>
    /// <response code="401">Chua xac thuc.</response>
    [HttpGet("calendar")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<BookingCalendarItem>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<ActionResult<ApiResponse<object>>> GetCalendar(
        [FromQuery] BookingCalendarRequest request,
        CancellationToken ct)
        => Ok(ApiResponse<object>.Ok(await bookingService.GetCalendarAsync(request, ct)));
    
    /// <summary>
    /// Tao moi mot lich dat ban.
    /// </summary>
    /// <remarks>
    /// Request mau:
    ///
    ///     POST /api/bookings
    ///     {
    ///        "customerName": "Nguyen Van A",
    ///        "phoneNumber": "0987654321",
    ///        "tableId": 1,
    ///        "tableTypeId": 1,
    ///        "startTimeUtc": "2023-12-01T10:00:00Z",
    ///        "endTimeUtc": "2023-12-01T12:00:00Z",
    ///        "numberOfGuests": 4
    ///     }
    ///
    /// He thong se tu dong su dung CustomerId neu duoc cung cap, hoac tao moi/cap nhat thong tin Khach hang dua tren so dien thoai (PhoneNumber).
    /// </remarks>
    /// <param name="request">Thong tin lich dat ban.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Lich dat ban vua duoc tao.</returns>
    [HttpPost] [AllowAnonymous] public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] CreateBookingRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await bookingService.CreateAsync(request, ct)));

    [HttpPost("public")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> CreatePublic([FromBody] CreateBookingRequest request, CancellationToken ct) =>
        StatusCode(201, ApiResponse<object>.Ok(await bookingService.CreatePublicAsync(request, ct)));

    [HttpPut("{id:long}")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff)]
    public async Task<ActionResult<ApiResponse<object>>> Update(long id, [FromBody] UpdateBookingRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await bookingService.UpdateAsync(id, request, ct)));
    
    /// <summary>
    /// Xac nhan mot lich dat ban dang o trang thai Pending.
    /// </summary>
    /// <param name="id">ID cua lich dat ban can xac nhan.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Lich dat ban da duoc cap nhat.</returns>
    [HttpPut("{id:int}/confirm")] 
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + ",Staff")] 
    public async Task<ActionResult<ApiResponse<object>>> Confirm(int id, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await bookingService.ConfirmAsync(id, User.GetUserId(), ct)));

    [HttpPost("{id:long}/approve")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
    public async Task<ActionResult<ApiResponse<object>>> Approve(long id, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await bookingService.ApproveAsync(id, User.GetUserId(), ct)));

    [HttpPost("{id:long}/deposit/submit-transfer")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> SubmitDepositTransfer(long id, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await bookingService.SubmitDepositTransferAsync(id, ct)));

    [HttpPost("{id:long}/deposit/confirm")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff)]
    public async Task<ActionResult<ApiResponse<object>>> ConfirmDeposit(long id, [FromBody] ConfirmDepositRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await bookingService.ConfirmDepositAsync(id, request, User.GetUserId(), ct)));

    [HttpPost("{id:long}/deposit/reject")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff)]
    public async Task<ActionResult<ApiResponse<object>>> RejectDepositTransfer(long id, [FromBody] RejectDepositTransferRequest? request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await bookingService.RejectDepositTransferAsync(id, request ?? new RejectDepositTransferRequest(), User.GetUserId(), ct)));

    [HttpPost("{id:long}/deposit/mock-pay")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
    public async Task<ActionResult<ApiResponse<object>>> MockPayDeposit(long id, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await bookingService.MockPayDepositAsync(id, ct)));
    
    /// <summary>
    /// Huy mot lich dat ban (Pending hoac Confirmed).
    /// </summary>
    /// <param name="id">ID cua lich dat ban can huy.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Lich dat ban da duoc cap nhat.</returns>
    [HttpPut("{id:int}/cancel")] 
    [Authorize] 
    public async Task<ActionResult<ApiResponse<object>>> Cancel(int id, [FromBody] CancelBookingRequest? request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await bookingService.CancelAsync(id, request ?? new CancelBookingRequest(), ct)));

    [HttpPut("{id:int}/no-show")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff)]
    public async Task<ActionResult<ApiResponse<object>>> NoShow(int id, [FromBody] NoShowBookingRequest? request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await bookingService.MarkNoShowAsync(id, request ?? new NoShowBookingRequest(), ct)));

    [HttpPatch("{id:int}/completed")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff)]
    public async Task<ActionResult<ApiResponse<object>>> Completed(int id, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await bookingService.MarkCompletedAsync(id, ct)));

    [HttpPost("{id:long}/start-session")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff)]
    public async Task<ActionResult<ApiResponse<object>>> StartSession(
        long id,
        [FromBody] StartSessionRequest? request,
        CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created,
            ApiResponse<object>.Ok(
                await sessionService.StartFromBookingAsync(id, request?.TableId, User.GetUserId(), ct),
                "Session started from booking."));

    [HttpGet("availability")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> Availability([FromQuery] BookingAvailabilityRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await bookingService.GetAvailabilityAsync(request, ct)));

    /// <summary>
    /// Lấy danh sách khoảng thời gian đã bị đặt của một bàn cụ thể trong ngày. Không tiết lộ thông tin khách hàng.
    /// </summary>
    /// <param name="tableId">ID của bàn.</param>
    /// <param name="date">Ngày cần xem (format YYYY-MM-DD).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Danh sách các khoảng thời gian đã bị đặt.</returns>
    [HttpGet("public/calendar")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PublicBookingSlotDto>>), 200)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PublicBookingSlotDto>>>> GetPublicCalendar(
        [FromQuery] long tableId, 
        [FromQuery] DateTime date, 
        CancellationToken ct) =>
        Ok(ApiResponse<IEnumerable<PublicBookingSlotDto>>.Ok(await bookingService.GetPublicCalendarAsync(tableId, date, ct)));

    
    /// <summary>
    /// Xoa vinh vien mot lich dat ban khoi database (Chi danh cho Admin/Manager).
    /// </summary>
    /// <param name="id">ID cua lich dat ban.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Thong bao thanh cong.</returns>
    [HttpDelete("{id:int}")] [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)] public async Task<ActionResult<ApiResponse<object>>> Delete(int id, CancellationToken ct) { await crud.DeleteBookingAsync(id, ct); return Ok(ApiResponse<object>.Ok(new { }, "Deleted")); }
}
