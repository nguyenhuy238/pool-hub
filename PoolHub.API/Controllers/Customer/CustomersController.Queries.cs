using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Customer;
using PoolHub.Shared;

namespace PoolHub.API.Controllers;

public partial class CustomersController
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
        => Ok(ApiResponse<object>.Ok(await _customerService.GetCustomersAsync(request, ct)));

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
        => Ok(ApiResponse<object>.Ok(await _customerService.GetCustomerAsync(id, ct)));
}
