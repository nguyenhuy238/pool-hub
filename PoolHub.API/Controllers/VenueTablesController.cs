using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Venue;
using PoolHub.Core.Interfaces;
using PoolHub.Shared;
using PoolHub.Shared.Constants;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/venue-tables")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff + "," + RoleConstants.Cashier)]
public class VenueTablesController(ICrudService s) : ControllerBase
{
    /// <summary>
    /// Lấy danh sách các bàn chơi (Venue Tables) có phân trang.
    /// </summary>
    /// <param name="r">Các tham số phân trang.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Danh sách các bàn chơi.</returns>
    [HttpGet] [AllowAnonymous] public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] PaginationRequest r, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetVenueTablesAsync(r, ct)));
    
    /// <summary>
    /// Lấy thông tin chi tiết một bàn chơi theo ID.
    /// </summary>
    /// <param name="id">ID của bàn chơi.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Chi tiết bàn chơi.</returns>
    [HttpGet("{id:int}")] [AllowAnonymous] public async Task<ActionResult<ApiResponse<object>>> GetById(int id, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetVenueTableAsync(id, ct)));
    
    /// <summary>
    /// Tạo mới một bàn chơi.
    /// </summary>
    /// <remarks>
    /// Request mẫu:
    ///
    ///     POST /api/venue-tables
    ///     {
    ///        "zoneId": 1,
    ///        "tableTypeId": 1,
    ///        "tableCode": "T01",
    ///        "tableName": "Bàn số 1",
    ///        "capacity": 4,
    ///        "operationalStatus": 1,
    ///        "isActive": true
    ///     }
    /// </remarks>
    /// <param name="dto">Thông tin bàn chơi.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Bàn chơi vừa tạo.</returns>
    [HttpPost] [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)] public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] VenueTableDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.CreateVenueTableAsync(dto, ct)));
    
    /// <summary>
    /// Cập nhật thông tin một bàn chơi.
    /// </summary>
    /// <param name="id">ID của bàn chơi.</param>
    /// <param name="dto">Dữ liệu cần cập nhật.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Bàn chơi đã cập nhật.</returns>
    [HttpPut("{id:int}")] [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)] public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] VenueTableDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.UpdateVenueTableAsync(id, dto, ct)));
    
    /// <summary>
    /// Xóa mềm một bàn chơi.
    /// </summary>
    /// <param name="id">ID của bàn chơi.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Thông báo thành công.</returns>
    [HttpDelete("{id:int}")] [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)] public async Task<ActionResult<ApiResponse<object>>> Delete(int id, CancellationToken ct) { await s.DeleteVenueTableAsync(id, ct); return Ok(ApiResponse<object>.Ok(new { }, "Deleted")); }
}
