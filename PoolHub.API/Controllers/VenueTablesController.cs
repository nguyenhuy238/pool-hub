using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Venue;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/venue-tables")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff + "," + RoleConstants.Cashier)]
public class VenueTablesController(IVenueService s) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] PaginationRequest r, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetVenueTablesAsync(r, ct)));
    [HttpGet("{id:int}")] public async Task<ActionResult<ApiResponse<object>>> GetById(int id, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetVenueTableAsync(id, ct)));
    [HttpPost] [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)] public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] VenueTableDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.CreateVenueTableAsync(dto, ct)));
    [HttpPut("{id:int}")] [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)] public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] VenueTableDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.UpdateVenueTableAsync(id, dto, ct)));
    [HttpDelete("{id:int}")] [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)] public async Task<ActionResult<ApiResponse<object>>> Delete(int id, CancellationToken ct) { await s.DeleteVenueTableAsync(id, ct); return Ok(ApiResponse<object>.Ok(new { }, "Deleted")); }
}
