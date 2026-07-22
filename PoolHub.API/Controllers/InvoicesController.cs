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
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<ApiResponse<InvoiceDetailDto>>> GetById(long id, CancellationToken ct)
    {
        Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";
        return Ok(ApiResponse<InvoiceDetailDto>.Ok(await invoiceService.GetInvoiceDetailAsync(id, ct)));
    }

    [HttpGet("payment-methods")]
    public async Task<ActionResult<ApiResponse<List<PaymentMethodDto>>>> GetPaymentMethods(CancellationToken ct) =>
        Ok(ApiResponse<List<PaymentMethodDto>>.Ok(await invoiceService.GetPaymentMethodsAsync(ct)));

    [HttpPost("generate/{sessionId:long}")] 
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> Generate(long sessionId, CancellationToken ct) => 
        Ok(ApiResponse<InvoiceDto>.Ok(await invoiceService.GenerateFromSessionAsync(sessionId, User.GetUserId(), ct)));

    [HttpPost("payments")] 
    public async Task<ActionResult<ApiResponse<CreatePaymentResponse>>> Payment([FromBody] CreatePaymentRequest request, CancellationToken ct) 
    { 
        var response = await invoiceService.CreatePaymentAsync(request, User.GetUserId(), ct); 
        return Ok(ApiResponse<CreatePaymentResponse>.Ok(response, "Payment created")); 
    }

    [HttpPost("{id:long}/discounts")]
    public async Task<ActionResult<ApiResponse<object>>> ApplyDiscount(long id, [FromBody] ApplyDiscountRequest request, CancellationToken ct)
    {
        await invoiceService.ApplyDiscountAsync(id, request, User.GetUserId(), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Discount applied"));
    }

    [HttpDelete("{id:long}/discounts")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveDiscount(long id, CancellationToken ct)
    {
        await invoiceService.RemoveDiscountAsync(id, User.GetUserId(), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Discount removed"));
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

    [HttpGet("{id:long}/qr-code")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> GetQrCode(long id, CancellationToken ct)
    {
        var url = await invoiceService.GetVietQrUrlAsync(id, ct);
        return Redirect(url);
    }

    [HttpPut("{id:long}/products")]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> UpdateProducts(long id, [FromBody] UpdateInvoiceProductsRequest request, CancellationToken ct)
    {
        var result = await invoiceService.UpdateInvoiceProductsAsync(id, request, User.GetUserId(), ct);
        return Ok(ApiResponse<InvoiceDto>.Ok(result, "Products updated successfully."));
    }

    [HttpPut("{id:long}/customer")]
    public async Task<ActionResult<ApiResponse<InvoiceDetailDto>>> UpdateCustomer(long id, [FromBody] UpdateInvoiceCustomerRequest request, CancellationToken ct)
    {
        var result = await invoiceService.UpdateInvoiceCustomerAsync(id, request, User.GetUserId(), ct);
        return Ok(ApiResponse<InvoiceDetailDto>.Ok(result, "Customer updated successfully."));
    }
}
