using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Invoice;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Extensions;
using System.Threading;
using System.Threading.Tasks;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff + "," + RoleConstants.Cashier)]
public class InvoicesController(IInvoiceService invoiceService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] InvoiceQueryRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await invoiceService.GetInvoicesAsync(request, ct)));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<InvoiceDetailDto>>> GetById(long id, CancellationToken ct) => 
        Ok(ApiResponse<InvoiceDetailDto>.Ok(await invoiceService.GetInvoiceDetailAsync(id, ct)));

    [HttpGet("payment-methods")]
    public async Task<ActionResult<ApiResponse<List<PaymentMethodDto>>>> GetPaymentMethods(CancellationToken ct) =>
        Ok(ApiResponse<List<PaymentMethodDto>>.Ok(await invoiceService.GetPaymentMethodsAsync(ct)));

    [HttpPost("generate/{sessionId:long}")] 
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> Generate(long sessionId, CancellationToken ct) => 
        Ok(ApiResponse<InvoiceDto>.Ok(await invoiceService.GenerateFromSessionAsync(sessionId, User.GetUserId(), ct)));

    [HttpPost("payments")] 
    public async Task<ActionResult<ApiResponse<object>>> Payment([FromBody] CreatePaymentRequest request, CancellationToken ct) 
    { 
        await invoiceService.CreatePaymentAsync(request, User.GetUserId(), ct); 
        return Ok(ApiResponse<object>.Ok(new { }, "Payment created")); 
    }

    [HttpPost("{id:long}/discounts")]
    public async Task<ActionResult<ApiResponse<object>>> ApplyDiscount(long id, [FromBody] ApplyDiscountRequest request, CancellationToken ct)
    {
        await invoiceService.ApplyDiscountAsync(id, request, User.GetUserId(), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Discount applied"));
    }

    [HttpPost("{id:long}/cancel")]
    public async Task<ActionResult<ApiResponse<object>>> Cancel(long id, [FromBody] CancelInvoiceRequest request, CancellationToken ct)
    {
        await invoiceService.CancelInvoiceAsync(id, request.Reason, User.GetUserId(), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Invoice cancelled"));
    }

    [HttpGet("{id:long}/export-pdf")]
    public async Task<ActionResult<ApiResponse<object>>> ExportPdf(long id, CancellationToken ct)
    {
        var url = await invoiceService.ExportPdfAsync(id, ct);
        return Ok(ApiResponse<object>.Ok(new { url }, "Exported successfully"));
    }
}
