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
    [HttpGet] [Authorize] public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] PaginationRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await crud.GetBookingsCrudAsync(request, ct)));
    [HttpGet("{id:int}")] [Authorize] public async Task<ActionResult<ApiResponse<object>>> GetById(int id, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await crud.GetBookingAsync(id, ct)));
    [HttpPost] [AllowAnonymous] public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] CreateBookingRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await bookingService.CreateAsync(request, ct)));
    [HttpDelete("{id:int}")] [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)] public async Task<ActionResult<ApiResponse<object>>> Delete(int id, CancellationToken ct) { await crud.DeleteBookingAsync(id, ct); return Ok(ApiResponse<object>.Ok(new { }, "Deleted")); }
}
