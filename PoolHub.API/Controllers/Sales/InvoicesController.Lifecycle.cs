using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Invoice;
using PoolHub.Shared;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers;

public partial class InvoicesController
{
    [HttpPost("{id:long}/cancel")]
    public async Task<ActionResult<ApiResponse<object>>> Cancel(long id, [FromBody] CancelInvoiceRequest request, CancellationToken ct)
    {
        await _invoiceService.CancelInvoiceAsync(id, request.Reason, User.GetUserId(), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Invoice cancelled"));
    }
}
