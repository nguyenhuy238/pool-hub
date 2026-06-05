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
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
public class TableTypesController(ICrudService s) : ControllerBase
{
    /// <summary>
    /// Lấy danh sách các loại bàn (Table Types) có phân trang.
    /// </summary>
    /// <param name="r">Các tham số phân trang.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Danh sách các loại bàn.</returns>
    [HttpGet] [AllowAnonymous] public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] PaginationRequest r, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetTableTypesAsync(r, ct)));
    
    /// <summary>
    /// Lấy thông tin chi tiết một loại bàn theo ID.
    /// </summary>
    /// <param name="id">ID của loại bàn.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Chi tiết loại bàn.</returns>
    [HttpGet("{id:int}")] [AllowAnonymous] public async Task<ActionResult<ApiResponse<object>>> GetById(int id, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetTableTypeAsync(id, ct)));
    
    /// <summary>
    /// Tạo mới một loại bàn.
    /// </summary>
    /// <param name="dto">Thông tin loại bàn.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Loại bàn vừa tạo.</returns>
    [HttpPost] public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] TableTypeDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.CreateTableTypeAsync(dto, ct)));
    
    /// <summary>
    /// Cập nhật thông tin một loại bàn.
    /// </summary>
    /// <param name="id">ID của loại bàn.</param>
    /// <param name="dto">Dữ liệu cần cập nhật.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Loại bàn đã cập nhật.</returns>
    [HttpPut("{id:int}")] public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] TableTypeDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.UpdateTableTypeAsync(id, dto, ct)));
    
    /// <summary>
    /// Xóa mềm một loại bàn.
    /// </summary>
    /// <param name="id">ID của loại bàn.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Thông báo thành công.</returns>
    [HttpDelete("{id:int}")] public async Task<ActionResult<ApiResponse<object>>> Delete(int id, CancellationToken ct) { await s.DeleteTableTypeAsync(id, ct); return Ok(ApiResponse<object>.Ok(new { }, "Deleted")); }
}
