using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Booking;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController(IBookingService bookingService) : ControllerBase
{
    [HttpGet] [Authorize] public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] BookingFilterRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await bookingService.GetBookingsAsync(request, ct)));
    [HttpGet("{id:int}")] [Authorize] public async Task<ActionResult<ApiResponse<object>>> GetById(int id, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await bookingService.GetBookingAsync(id, ct)));
    [HttpPost] [AllowAnonymous] public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] CreateBookingRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await bookingService.CreateAsync(request, ct)));
    [HttpPut("{id:int}/confirm")] [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff)] public async Task<ActionResult<ApiResponse<object>>> Confirm(int id, CancellationToken ct) { await bookingService.ConfirmAsync(id, ct); return Ok(ApiResponse<object>.Ok(new { }, "Confirmed")); }
    [HttpPut("{id:int}/cancel")] [Authorize] public async Task<ActionResult<ApiResponse<object>>> Cancel(int id, CancellationToken ct) { await bookingService.CancelAsync(id, ct); return Ok(ApiResponse<object>.Ok(new { }, "Cancelled")); }
    [HttpDelete("{id:int}")] [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)] public async Task<ActionResult<ApiResponse<object>>> Delete(int id, CancellationToken ct) { await bookingService.DeleteBookingAsync(id, ct); return Ok(ApiResponse<object>.Ok(new { }, "Deleted")); }
}
