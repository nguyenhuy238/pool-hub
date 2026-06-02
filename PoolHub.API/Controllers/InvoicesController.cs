using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Invoice;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff + "," + RoleConstants.Cashier)]
public class InvoicesController(IInvoiceService invoiceService) : ControllerBase
{
    [HttpPost("generate/{sessionId:int}")] public async Task<ActionResult<ApiResponse<object>>> Generate(int sessionId, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await invoiceService.GenerateFromSessionAsync(sessionId, ct)));
    [HttpPost("payments")] public async Task<ActionResult<ApiResponse<object>>> Payment([FromBody] CreatePaymentRequest request, CancellationToken ct) { await invoiceService.CreatePaymentAsync(request, ct); return Ok(ApiResponse<object>.Ok(new { }, "Payment created")); }
}
