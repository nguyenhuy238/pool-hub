using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Venue;
using PoolHub.Core.Interfaces;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/venue-tables")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff + "," + RoleConstants.Cashier, Policy = PermissionConstants.VenueManage)]
public class VenueTablesController(ICrudService s, IVenueService venueService) : ControllerBase
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
    /// Lấy sơ đồ toàn bộ venue theo cấu trúc phân cấp Floor → Zone → Table.
    /// </summary>
    /// <remarks>
    /// Trả về cây cấu trúc đầy đủ của venue:
    /// - Danh sách tầng (Floor), mỗi tầng gồm các khu vực (Zone)
    /// - Mỗi khu vực gồm các bàn (VenueTable) với trạng thái hoạt động realtime
    /// - Trạng thái bàn phản ánh session đang chạy: 1=Available, 2=Occupied, 3=Reserved, 4=Maintenance
    /// - Trường `activeSessionId` cho biết session đang chạy trên bàn đó (null = không có)
    /// - Thống kê nhanh: totalTables, availableTables, occupiedTables
    ///
    /// Ví dụ kết quả:
    ///
    ///     {
    ///       "data": {
    ///         "floors": [
    ///           {
    ///             "floorId": 1,
    ///             "floorName": "Tầng 1",
    ///             "zones": [
    ///               {
    ///                 "zoneId": 1,
    ///                 "zoneName": "Khu VIP",
    ///                 "tables": [
    ///                   {
    ///                     "tableId": 1,
    ///                     "tableCode": "T01",
    ///                     "tableName": "Bàn số 1",
    ///                     "operationalStatus": 2,
    ///                     "operationalStatusLabel": "Occupied",
    ///                     "activeSessionId": 5
    ///                   }
    ///                 ]
    ///               }
    ///             ]
    ///           }
    ///         ],
    ///         "totalTables": 20,
    ///         "availableTables": 15,
    ///         "occupiedTables": 5
    ///       }
    ///     }
    /// </remarks>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Sơ đồ venue kèm trạng thái realtime.</returns>
    /// <response code="200">Trả về sơ đồ venue thành công.</response>
    /// <response code="401">Chưa xác thực.</response>
    [HttpGet("layout")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<VenueLayoutResponse>), 200)]
    public async Task<ActionResult<ApiResponse<object>>> GetLayout(CancellationToken ct)
        => Ok(ApiResponse<object>.Ok(await venueService.GetLayoutAsync(ct)));
    
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

