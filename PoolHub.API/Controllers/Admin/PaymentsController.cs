using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Admin;
using PoolHub.Core.DTOs.Invoice;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers;

[ApiController, Route("api/payments")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff, Policy = PermissionConstants.PaymentsManage)]
public class PaymentsController(IAdminManagementService admin, IInvoiceService invoices) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] PaymentQueryRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await admin.GetPaymentsAsync(request, ct)));
    [HttpPost] public async Task<IActionResult> Create(CreatePaymentRequest request, CancellationToken ct) { await invoices.CreatePaymentAsync(request, User.GetUserId(), ct); return StatusCode(201, ApiResponse<object>.Ok(new { }, "Payment recorded.")); }
    [HttpPost("{id:long}/refund")] public async Task<IActionResult> Refund(long id, [FromBody] RefundPaymentRequest request, CancellationToken ct) { await invoices.RefundPaymentAsync(id, request.Reason, User.GetUserId(), ct); return Ok(ApiResponse<object>.Ok(new { }, "Payment refunded.")); }
}
