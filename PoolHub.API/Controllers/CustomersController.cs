using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Customer;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;

namespace PoolHub.API.Controllers;

/// <summary>
/// Quan ly thong tin khach hang.
/// </summary>
[ApiController]
[Route("api/customers")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff)]
public class CustomersController(ICustomerService customerService) : ControllerBase
{
    /// <summary>
    /// Lay danh sach khach hang co phan trang va tim kiem.
    /// </summary>
    /// <remarks>
    /// Cho phep tim kiem theo ten hoac so dien thoai, loc theo trang thai.
    ///
    /// **Cach test tren Swagger:**
    /// 1. Xac thuc bang JWT (role: Admin, Manager, hoac Staff)
    /// 2. Truyen query params:
    ///    - `search`: tim kiem theo ten hoac SĐT (tuy chon)
    ///    - `status`: true = Active, false = Inactive (tuy chon, de trong = lay tat ca)
    ///    - `pageNumber`, `pageSize`: phan trang
    ///
    /// **Vi du request:**
    ///
    ///     GET /api/customers?search=Nguyen&amp;status=true&amp;pageNumber=1&amp;pageSize=20
    ///
    /// **Vi du response:**
    ///
    ///     {
    ///       "data": {
    ///         "items": [
    ///           {
    ///             "customerId": 1,
    ///             "fullName": "Nguyen Van A",
    ///             "phoneNumber": "0987654321",
    ///             "email": "nguyenvana@example.com",
    ///             "status": true,
    ///             "totalBookings": 5
    ///           }
    ///         ],
    ///         "totalCount": 1,
    ///         "pageNumber": 1,
    ///         "pageSize": 20
    ///       }
    ///     }
    /// </remarks>
    /// <param name="request">Query params: search, status, pageNumber, pageSize.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Danh sach khach hang.</returns>
    /// <response code="200">Tra ve danh sach thanh cong.</response>
    /// <response code="401">Chua xac thuc.</response>
    /// <response code="403">Khong co quyen truy cap.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CustomerDto>>), 200)]
    public async Task<ActionResult<ApiResponse<object>>> GetCustomers(
        [FromQuery] CustomerQueryRequest request,
        CancellationToken ct)
        => Ok(ApiResponse<object>.Ok(await customerService.GetCustomersAsync(request, ct)));

    /// <summary>
    /// Lay thong tin chi tiet mot khach hang theo ID.
    /// </summary>
    /// <remarks>
    /// Tra ve thong tin day du cua khach hang, bao gom tong so lan dat ban.
    ///
    /// **Cach test tren Swagger:**
    /// 1. Xac thuc bang JWT
    /// 2. Truyen `id` = ID cua khach hang (lay tu endpoint GET /api/customers)
    ///
    /// **Vi du response:**
    ///
    ///     {
    ///       "data": {
    ///         "customerId": 1,
    ///         "fullName": "Nguyen Van A",
    ///         "phoneNumber": "0987654321",
    ///         "email": "nguyenvana@example.com",
    ///         "note": "Khach hang VIP",
    ///         "status": true,
    ///         "createdAtUtc": "2026-01-15T08:00:00Z",
    ///         "totalBookings": 5
    ///       }
    ///     }
    /// </remarks>
    /// <param name="id">ID cua khach hang.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Thong tin chi tiet khach hang.</returns>
    /// <response code="200">Tra ve thong tin khach hang.</response>
    /// <response code="404">Khong tim thay khach hang.</response>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<ActionResult<ApiResponse<object>>> GetCustomer(long id, CancellationToken ct)
        => Ok(ApiResponse<object>.Ok(await customerService.GetCustomerAsync(id, ct)));

    /// <summary>
    /// Cap nhat thong tin khach hang.
    /// </summary>
    /// <remarks>
    /// Cap nhat cac truong: fullName, email, note, status.
    /// Luu y: Khong the thay doi PhoneNumber qua endpoint nay (dung de tao booking moi).
    ///
    /// **Cach test tren Swagger:**
    /// 1. Xac thuc bang JWT (role: Admin hoac Manager)
    /// 2. Truyen `id` va request body
    ///
    /// **Vi du request body:**
    ///
    ///     PUT /api/customers/1
    ///     {
    ///       "fullName": "Nguyen Van B",
    ///       "email": "nguyenvanb@example.com",
    ///       "note": "Khach hang than thiet",
    ///       "status": true
    ///     }
    ///
    /// **Vi du response:**
    ///
    ///     {
    ///       "data": {
    ///         "customerId": 1,
    ///         "fullName": "Nguyen Van B",
    ///         "phoneNumber": "0987654321",
    ///         "email": "nguyenvanb@example.com",
    ///         "note": "Khach hang than thiet",
    ///         "status": true,
    ///         "totalBookings": 5
    ///       }
    ///     }
    /// </remarks>
    /// <param name="id">ID cua khach hang.</param>
    /// <param name="request">Du lieu can cap nhat.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Thong tin khach hang sau khi cap nhat.</returns>
    /// <response code="200">Cap nhat thanh cong.</response>
    /// <response code="400">Du lieu dau vao khong hop le.</response>
    /// <response code="404">Khong tim thay khach hang.</response>
    [HttpPut("{id:long}")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<ActionResult<ApiResponse<object>>> UpdateCustomer(
        long id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken ct)
        => Ok(ApiResponse<object>.Ok(await customerService.UpdateCustomerAsync(id, request, ct)));
}
