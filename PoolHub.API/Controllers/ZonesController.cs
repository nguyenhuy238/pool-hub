using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Venue;
using PoolHub.Core.Interfaces;
using PoolHub.Shared;
using PoolHub.Shared.Constants;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/zones")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager, Policy = PermissionConstants.VenueManage)]
public class ZonesController(ICrudService s) : ControllerBase
{
    /// <summary>
    /// Lấy danh sách các khu vực (Zones) có phân trang.
    /// </summary>
    /// <param name="r">Các tham số phân trang.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Danh sách các khu vực.</returns>
    [HttpGet] [AllowAnonymous] public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] PaginationRequest r, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetZonesAsync(r, ct)));
    
    /// <summary>
    /// Lấy thông tin chi tiết một khu vực theo ID.
    /// </summary>
    /// <param name="id">ID của khu vực.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Chi tiết khu vực.</returns>
    [HttpGet("{id:int}")] [AllowAnonymous] public async Task<ActionResult<ApiResponse<object>>> GetById(int id, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetZoneAsync(id, ct)));
    
    /// <summary>
    /// Tạo mới một khu vực.
    /// </summary>
    /// <param name="dto">Thông tin khu vực.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Khu vực vừa tạo.</returns>
    [HttpPost] public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] ZoneDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.CreateZoneAsync(dto, ct)));
    
    /// <summary>
    /// Cập nhật thông tin một khu vực.
    /// </summary>
    /// <param name="id">ID của khu vực.</param>
    /// <param name="dto">Dữ liệu cần cập nhật.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Khu vực đã cập nhật.</returns>
    [HttpPut("{id:int}")] public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] ZoneDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.UpdateZoneAsync(id, dto, ct)));
    
    /// <summary>
    /// Xóa mềm một khu vực (Cập nhật IsActive = false).
    /// </summary>
    /// <param name="id">ID của khu vực.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Thông báo thành công.</returns>
    [HttpDelete("{id:int}")] public async Task<ActionResult<ApiResponse<object>>> Delete(int id, CancellationToken ct) { await s.DeleteZoneAsync(id, ct); return Ok(ApiResponse<object>.Ok(new { }, "Deleted")); }
}
