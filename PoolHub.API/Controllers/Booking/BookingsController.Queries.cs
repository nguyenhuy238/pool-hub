using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Booking;
using PoolHub.Core.DTOs.Common;
using PoolHub.Shared;
using PoolHub.Shared.Constants;

namespace PoolHub.API.Controllers;

public partial class BookingsController
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ApiResponse<PagedResult<BookingDto>>>> Get([FromQuery] BookingQueryRequest request, CancellationToken ct) =>
        Ok(ApiResponse<PagedResult<BookingDto>>.Ok(await _bookingService.GetBookingsAsync(request, ct)));

    [HttpGet("calendar")]
    [Authorize(Roles = RoleConstants.Operation)]
    public async Task<ActionResult<ApiResponse<PagedResult<BookingCalendarItem>>>> GetCalendar([FromQuery] BookingCalendarRequest request, CancellationToken ct) =>
        Ok(ApiResponse<PagedResult<BookingCalendarItem>>.Ok(await _bookingService.GetCalendarAsync(request, ct)));

    [HttpGet("{id:long}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<BookingDto>>> GetById(long id, CancellationToken ct) =>
        Ok(ApiResponse<BookingDto>.Ok(await _bookingService.GetByIdAsync(id, ct)));
}
