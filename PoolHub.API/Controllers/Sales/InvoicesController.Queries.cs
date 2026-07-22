using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Invoice;
using PoolHub.Shared;

namespace PoolHub.API.Controllers;

public partial class InvoicesController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] InvoiceQueryRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await _invoiceService.GetInvoicesAsync(request, ct)));

    [HttpGet("{id:long}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<ApiResponse<InvoiceDetailDto>>> GetById(long id, CancellationToken ct)
    {
        Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";
        return Ok(ApiResponse<InvoiceDetailDto>.Ok(await _invoiceService.GetInvoiceDetailAsync(id, ct)));
    }

    [HttpGet("payment-methods")]
    public async Task<ActionResult<ApiResponse<List<PaymentMethodDto>>>> GetPaymentMethods(CancellationToken ct) =>
        Ok(ApiResponse<List<PaymentMethodDto>>.Ok(await _invoiceService.GetPaymentMethodsAsync(ct)));
}
