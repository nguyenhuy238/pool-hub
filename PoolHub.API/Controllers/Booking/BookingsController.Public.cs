using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Booking;
using PoolHub.Core.DTOs.Common;
using PoolHub.Shared;

namespace PoolHub.API.Controllers;

public partial class BookingsController
{
    [HttpGet("availability")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<AvailableTableDto>>>> GetAvailability([FromQuery] BookingAvailabilityRequest request, CancellationToken ct) =>
        Ok(ApiResponse<List<AvailableTableDto>>.Ok(await _bookingService.GetAvailabilityAsync(request, ct)));

    [HttpGet("public/calendar")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IEnumerable<PublicBookingSlotDto>>>> GetPublicCalendar([FromQuery] long tableId, [FromQuery] DateTime date, CancellationToken ct) =>
        Ok(ApiResponse<IEnumerable<PublicBookingSlotDto>>.Ok(await _bookingService.GetPublicCalendarAsync(tableId, date, ct)));

    [HttpGet("public/{id:long}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<BookingDto>>> GetPublicBookingById(long id, CancellationToken ct) =>
        Ok(ApiResponse<BookingDto>.Ok(await _bookingService.GetByIdAsync(id, ct)));

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] CreateBookingRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await _bookingService.CreateAsync(request, ct)));

    [HttpPost("public")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<BookingDto>>> CreatePublic([FromBody] CreateBookingRequest request, CancellationToken ct) =>
        Ok(ApiResponse<BookingDto>.Ok(await _bookingService.CreatePublicAsync(request, ct)));
}
