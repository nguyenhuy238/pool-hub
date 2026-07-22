using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Invoice;
using PoolHub.Shared;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers;

public partial class InvoicesController
{
    [HttpPut("{id:long}/products")]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> UpdateProducts(long id, [FromBody] UpdateInvoiceProductsRequest request, CancellationToken ct)
    {
        var result = await _invoiceService.UpdateInvoiceProductsAsync(id, request, User.GetUserId(), ct);
        return Ok(ApiResponse<InvoiceDto>.Ok(result, "Products updated successfully."));
    }

    [HttpPut("{id:long}/customer")]
    public async Task<ActionResult<ApiResponse<InvoiceDetailDto>>> UpdateCustomer(long id, [FromBody] UpdateInvoiceCustomerRequest request, CancellationToken ct)
    {
        var result = await _invoiceService.UpdateInvoiceCustomerAsync(id, request, User.GetUserId(), ct);
        return Ok(ApiResponse<InvoiceDetailDto>.Ok(result, "Customer updated successfully."));
    }
}
