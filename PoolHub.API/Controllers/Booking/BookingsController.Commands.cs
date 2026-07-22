using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Booking;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers;

public partial class BookingsController
{
    [HttpPut("{id:long}")]
    [Authorize(Roles = RoleConstants.Operation)]
    public async Task<ActionResult<ApiResponse<BookingDto>>> Update(long id, [FromBody] UpdateBookingRequest request, CancellationToken ct) =>
        Ok(ApiResponse<BookingDto>.Ok(await _bookingService.UpdateAsync(id, request, ct)));

    [HttpPut("{id:long}/confirm")]
    [Authorize(Roles = RoleConstants.Operation)]
    public async Task<ActionResult<ApiResponse<BookingDto>>> Confirm(long id, CancellationToken ct) =>
        Ok(ApiResponse<BookingDto>.Ok(await _bookingService.ConfirmAsync(id, User.GetUserId(), ct), "Booking confirmed"));

    [HttpPost("{id:long}/approve")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
    public async Task<ActionResult<ApiResponse<BookingDto>>> Approve(long id, CancellationToken ct) =>
        Ok(ApiResponse<BookingDto>.Ok(await _bookingService.ApproveAsync(id, User.GetUserId(), ct), "Booking approved"));

    [HttpDelete("{id:long}")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(long id, CancellationToken ct)
    {
        await _crud.DeleteBookingAsync(id, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Deleted"));
    }
}
