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
    private const string OperationRoles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff + "," + RoleConstants.Cashier;

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] PaginationRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await crud.GetBookingsCrudAsync(request, ct)));

    [HttpGet("calendar")]
    [Authorize(Roles = OperationRoles)]
    public async Task<ActionResult<ApiResponse<PagedResult<BookingCalendarItem>>>> GetCalendar([FromQuery] BookingCalendarRequest request, CancellationToken ct) =>
        Ok(ApiResponse<PagedResult<BookingCalendarItem>>.Ok(await bookingService.GetCalendarAsync(request, ct)));

    [HttpGet("availability")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<AvailableTableDto>>>> GetAvailability([FromQuery] BookingAvailabilityRequest request, CancellationToken ct) =>
        Ok(ApiResponse<List<AvailableTableDto>>.Ok(await bookingService.GetAvailabilityAsync(request, ct)));

    [HttpGet("public/calendar")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IEnumerable<PublicBookingSlotDto>>>> GetPublicCalendar([FromQuery] long tableId, [FromQuery] DateTime date, CancellationToken ct) =>
        Ok(ApiResponse<IEnumerable<PublicBookingSlotDto>>.Ok(await bookingService.GetPublicCalendarAsync(tableId, date, ct)));

    [HttpGet("{id:long}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> GetById(long id, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await crud.GetBookingAsync(id, ct)));

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] CreateBookingRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await bookingService.CreateAsync(request, ct)));

    [HttpPost("public")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<BookingDto>>> CreatePublic([FromBody] CreateBookingRequest request, CancellationToken ct) =>
        Ok(ApiResponse<BookingDto>.Ok(await bookingService.CreatePublicAsync(request, ct)));

    [HttpPut("{id:long}")]
    [Authorize(Roles = OperationRoles)]
    public async Task<ActionResult<ApiResponse<BookingDto>>> Update(long id, [FromBody] UpdateBookingRequest request, CancellationToken ct) =>
        Ok(ApiResponse<BookingDto>.Ok(await bookingService.UpdateAsync(id, request, ct)));

    [HttpPut("{id:long}/confirm")]
    [Authorize(Roles = OperationRoles)]
    public async Task<ActionResult<ApiResponse<BookingDto>>> Confirm(long id, CancellationToken ct) =>
        Ok(ApiResponse<BookingDto>.Ok(await bookingService.ConfirmAsync(id, User.GetUserId(), ct), "Booking confirmed"));

    [HttpPost("{id:long}/approve")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
    public async Task<ActionResult<ApiResponse<BookingDto>>> Approve(long id, CancellationToken ct) =>
        Ok(ApiResponse<BookingDto>.Ok(await bookingService.ApproveAsync(id, User.GetUserId(), ct), "Booking approved"));

    [HttpPost("{id:long}/deposit/submit-transfer")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<BookingDto>>> SubmitDepositTransfer(long id, CancellationToken ct) =>
        Ok(ApiResponse<BookingDto>.Ok(await bookingService.SubmitDepositTransferAsync(id, ct), "Deposit transfer submitted"));

    [HttpPost("{id:long}/deposit/confirm")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Cashier)]
    public async Task<ActionResult<ApiResponse<BookingDto>>> ConfirmDeposit(long id, [FromBody] ConfirmDepositRequest request, CancellationToken ct) =>
        Ok(ApiResponse<BookingDto>.Ok(await bookingService.ConfirmDepositAsync(id, request, User.GetUserId(), ct), "Deposit confirmed"));

    [HttpPost("{id:long}/deposit/reject")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Cashier)]
    public async Task<ActionResult<ApiResponse<BookingDto>>> RejectDepositTransfer(long id, [FromBody] RejectDepositTransferRequest request, CancellationToken ct) =>
        Ok(ApiResponse<BookingDto>.Ok(await bookingService.RejectDepositTransferAsync(id, request, User.GetUserId(), ct), "Deposit transfer rejected"));

    [HttpPost("{id:long}/deposit/mock-pay")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Cashier)]
    public async Task<ActionResult<ApiResponse<BookingDto>>> MockPayDeposit(long id, CancellationToken ct) =>
        Ok(ApiResponse<BookingDto>.Ok(await bookingService.MockPayDepositAsync(id, ct), "Deposit paid"));

    [HttpPut("{id:long}/cancel")]
    [Authorize(Roles = OperationRoles)]
    public async Task<ActionResult<ApiResponse<BookingDto>>> Cancel(long id, [FromBody] CancelBookingRequest request, CancellationToken ct) =>
        Ok(ApiResponse<BookingDto>.Ok(await bookingService.CancelAsync(id, request, ct), "Booking cancelled"));

    [HttpPut("{id:long}/no-show")]
    [Authorize(Roles = OperationRoles)]
    public async Task<ActionResult<ApiResponse<BookingDto>>> NoShow(long id, [FromBody] NoShowBookingRequest request, CancellationToken ct) =>
        Ok(ApiResponse<BookingDto>.Ok(await bookingService.MarkNoShowAsync(id, request, ct), "Booking marked no-show"));

    [HttpPut("{id:long}/complete")]
    [Authorize(Roles = OperationRoles)]
    public async Task<ActionResult<ApiResponse<BookingDto>>> Complete(long id, CancellationToken ct) =>
        Ok(ApiResponse<BookingDto>.Ok(await bookingService.MarkCompletedAsync(id, ct), "Booking completed"));

    [HttpPost("{id:long}/start-session")]
    [Authorize(Roles = OperationRoles)]
    public async Task<ActionResult<ApiResponse<SessionDto>>> StartSession(long id, [FromBody] StartSessionRequest request, CancellationToken ct) =>
        Ok(ApiResponse<SessionDto>.Ok(await sessionService.StartFromBookingAsync(id, request.TableId > 0 ? request.TableId : null, User.GetUserId(), ct), "Session started"));

    [HttpDelete("{id:long}")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(long id, CancellationToken ct)
    {
        await crud.DeleteBookingAsync(id, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Deleted"));
    }
}
