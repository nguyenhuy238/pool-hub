using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Customer;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers;

public partial class CustomersController
{
    [HttpPost]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
    public async Task<ActionResult<ApiResponse<object>>> CreateCustomer(
        [FromBody] CreateCustomerRequest request,
        CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created,
            ApiResponse<object>.Ok(await _customerService.CreateCustomerAsync(request, User.GetUserId(), ct), "Customer created."));

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
        => Ok(ApiResponse<object>.Ok(await _customerService.UpdateCustomerAsync(id, request, User.GetUserId(), ct)));
}
