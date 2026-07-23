using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers;

/// <summary>
/// Self-service endpoints for the signed-in customer. The customer id is always
/// resolved from the authenticated account on the server.
/// </summary>
[ApiController]
[Route("api/customer-portal")]
[Authorize(Roles = RoleConstants.Customer)]
public sealed class CustomerPortalController(
    ICustomerService customerService,
    IInvoiceService invoiceService) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<object>>> Me(CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await customerService.GetPortalProfileAsync(User.GetUserId(), ct)));

    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateMe(
        PoolHub.Core.DTOs.Customer.UpdateCustomerPortalProfileRequest request,
        CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(
            await customerService.UpdatePortalProfileAsync(User.GetUserId(), request, ct),
            "Cập nhật hồ sơ thành công."));

    [HttpGet("me/bookings")]
    public async Task<ActionResult<ApiResponse<object>>> Bookings(
        [FromQuery] PaginationRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await customerService.GetPortalBookingHistoryAsync(User.GetUserId(), request, ct)));

    [HttpGet("me/sessions")]
    public async Task<ActionResult<ApiResponse<object>>> Sessions(
        [FromQuery] PaginationRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await customerService.GetPortalSessionHistoryAsync(User.GetUserId(), request, ct)));

    [HttpGet("me/invoices")]
    public async Task<ActionResult<ApiResponse<object>>> Invoices(
        [FromQuery] PaginationRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await customerService.GetPortalInvoiceHistoryAsync(User.GetUserId(), request, ct)));

    [HttpGet("me/invoices/{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> InvoiceDetail(long id, CancellationToken ct)
    {
        var customerId = await customerService.ResolvePortalCustomerIdAsync(User.GetUserId(), ct);
        return Ok(ApiResponse<object>.Ok(
            await invoiceService.GetInvoiceDetailForCustomerAsync(id, customerId, ct)));
    }

    [HttpGet("me/vouchers")]
    public async Task<ActionResult<ApiResponse<object>>> Vouchers(
        [FromQuery] PaginationRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await customerService.GetPortalVouchersAsync(User.GetUserId(), request, ct)));

    [HttpGet("me/voucher-templates")]
    public async Task<ActionResult<ApiResponse<object>>> VoucherTemplates(CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(
            await customerService.GetPortalVoucherTemplatesAsync(User.GetUserId(), ct)));

    [HttpPost("me/vouchers/{templateId:long}/exchange")]
    public async Task<ActionResult<ApiResponse<object>>> ExchangeVoucher(
        long templateId, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(
            await customerService.ExchangePortalVoucherAsync(User.GetUserId(), templateId, ct),
            "Đổi voucher thành công."));

    [HttpGet("me/point-history")]
    public async Task<ActionResult<ApiResponse<object>>> PointHistory(
        [FromQuery] PaginationRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await customerService.GetPortalPointHistoryAsync(User.GetUserId(), request, ct)));
}
