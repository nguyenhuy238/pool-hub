using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Common;
using PoolHub.Shared;

namespace PoolHub.API.Controllers;

public partial class CustomersController
{
    [HttpGet("{id:long}/bookings")]
    public async Task<ActionResult<ApiResponse<object>>> Bookings(
        long id, [FromQuery] PaginationRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await _customerService.GetBookingHistoryAsync(id, request, ct)));

    [HttpGet("{id:long}/sessions")]
    public async Task<ActionResult<ApiResponse<object>>> Sessions(
        long id, [FromQuery] PaginationRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await _customerService.GetSessionHistoryAsync(id, request, ct)));

    [HttpGet("{id:long}/invoices")]
    public async Task<ActionResult<ApiResponse<object>>> Invoices(
        long id, [FromQuery] PaginationRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await _customerService.GetInvoiceHistoryAsync(id, request, ct)));
}
