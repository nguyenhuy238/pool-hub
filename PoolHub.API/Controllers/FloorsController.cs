using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Venue;
using PoolHub.Core.Interfaces;
using PoolHub.Shared;
using PoolHub.Shared.Constants;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/floors")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager, Policy = PermissionConstants.VenueManage)]
public class FloorsController(ICrudService s) : ControllerBase
{
    /// <summary>
    /// Lấy danh sách các tầng (Floors) có phân trang.
    /// </summary>
    /// <param name="r">Các tham số phân trang.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Danh sách các tầng.</returns>
    [HttpGet] [AllowAnonymous] public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] PaginationRequest r, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetFloorsAsync(r, ct)));
    
    /// <summary>
    /// Lấy thông tin chi tiết một tầng theo ID.
    /// </summary>
    /// <param name="id">ID của tầng.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Chi tiết tầng.</returns>
    [HttpGet("{id:int}")] [AllowAnonymous] public async Task<ActionResult<ApiResponse<object>>> GetById(int id, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetFloorAsync(id, ct)));
    
    /// <summary>
    /// Tạo mới một tầng.
    /// </summary>
    /// <remarks>
    /// Request mẫu:
    ///
    ///     POST /api/floors
    ///     {
    ///        "name": "Floor 2 - VIP",
    ///        "description": "Khu vực VIP",
    ///        "displayOrder": 2,
    ///        "isActive": true
    ///     }
    /// </remarks>
    /// <param name="dto">Thông tin tầng.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tầng vừa tạo.</returns>
    [HttpPost] public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] FloorDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.CreateFloorAsync(dto, ct)));
    
    /// <summary>
    /// Cập nhật thông tin một tầng.
    /// </summary>
    /// <param name="id">ID của tầng.</param>
    /// <param name="dto">Dữ liệu cần cập nhật.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tầng đã cập nhật.</returns>
    [HttpPut("{id:int}")] public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] FloorDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.UpdateFloorAsync(id, dto, ct)));
    
    /// <summary>
    /// Xóa mềm một tầng (Cập nhật IsActive = false).
    /// </summary>
    /// <param name="id">ID của tầng.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Thông báo thành công.</returns>
    [HttpDelete("{id:int}")] public async Task<ActionResult<ApiResponse<object>>> Delete(int id, CancellationToken ct) { await s.DeleteFloorAsync(id, ct); return Ok(ApiResponse<object>.Ok(new { }, "Deleted")); }
}
