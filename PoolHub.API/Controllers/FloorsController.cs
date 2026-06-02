using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Venue;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/floors")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
public class FloorsController(IVenueService s) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] PaginationRequest r, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetFloorsAsync(r, ct)));
    [HttpGet("{id:int}")] public async Task<ActionResult<ApiResponse<object>>> GetById(int id, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetFloorAsync(id, ct)));
    [HttpPost] public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] FloorDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.CreateFloorAsync(dto, ct)));
    [HttpPut("{id:int}")] public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] FloorDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.UpdateFloorAsync(id, dto, ct)));
    [HttpDelete("{id:int}")] public async Task<ActionResult<ApiResponse<object>>> Delete(int id, CancellationToken ct) { await s.DeleteFloorAsync(id, ct); return Ok(ApiResponse<object>.Ok(new { }, "Deleted")); }
}
