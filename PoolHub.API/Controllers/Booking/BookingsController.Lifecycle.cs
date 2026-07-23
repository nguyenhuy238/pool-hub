using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Booking;
using PoolHub.Core.DTOs.Session;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers;

public partial class BookingsController
{
    [HttpPut("{id:long}/cancel")]
    [Authorize(Roles = RoleConstants.Operation)]
    public async Task<ActionResult<ApiResponse<BookingDto>>> Cancel(long id, [FromBody] CancelBookingRequest request, CancellationToken ct) =>
        Ok(ApiResponse<BookingDto>.Ok(await _bookingService.CancelAsync(id, request, ct), "Booking cancelled"));

    [HttpPut("{id:long}/no-show")]
    [Authorize(Roles = RoleConstants.Operation)]
    public async Task<ActionResult<ApiResponse<BookingDto>>> NoShow(long id, [FromBody] NoShowBookingRequest request, CancellationToken ct) =>
        Ok(ApiResponse<BookingDto>.Ok(await _bookingService.MarkNoShowAsync(id, request, ct), "Booking marked no-show"));

    [HttpPut("{id:long}/complete")]
    [Authorize(Roles = RoleConstants.Operation)]
    public async Task<ActionResult<ApiResponse<BookingDto>>> Complete(long id, CancellationToken ct) =>
        Ok(ApiResponse<BookingDto>.Ok(await _bookingService.MarkCompletedAsync(id, ct), "Booking completed"));

    [HttpPost("{id:long}/start-session")]
    [Authorize(Roles = RoleConstants.Operation)]
    public async Task<ActionResult<ApiResponse<SessionDto>>> StartSession(long id, [FromBody] StartSessionRequest request, CancellationToken ct) =>
        Ok(ApiResponse<SessionDto>.Ok(await _sessionService.StartFromBookingAsync(id, request.TableId > 0 ? request.TableId : null, User.GetUserId(), ct), "Session started"));
}
