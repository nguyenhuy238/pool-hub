using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Venue;
using PoolHub.Core.Interfaces;
using PoolHub.Shared;
using PoolHub.Shared.Constants;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/table-types")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager, Policy = PermissionConstants.VenueManage)]
public class TableTypesController(ICrudService s) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] PaginationRequest r, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetTableTypesAsync(r, ct)));
    [HttpGet("{id:int}")] public async Task<ActionResult<ApiResponse<object>>> GetById(int id, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetTableTypeAsync(id, ct)));
    [HttpPost] public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] TableTypeDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.CreateTableTypeAsync(dto, ct)));
    [HttpPut("{id:int}")] public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] TableTypeDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.UpdateTableTypeAsync(id, dto, ct)));
    [HttpDelete("{id:int}")] public async Task<ActionResult<ApiResponse<object>>> Delete(int id, CancellationToken ct) { await s.DeleteTableTypeAsync(id, ct); return Ok(ApiResponse<object>.Ok(new { }, "Deleted")); }
}
