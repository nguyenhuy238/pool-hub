using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Invoice;
using PoolHub.Shared;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers;

public partial class InvoicesController
{
    [HttpPost("{id:long}/discounts")]
    public async Task<ActionResult<ApiResponse<object>>> ApplyDiscount(long id, [FromBody] ApplyDiscountRequest request, CancellationToken ct)
    {
        await _invoiceService.ApplyDiscountAsync(id, request, User.GetUserId(), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Discount applied"));
    }

    [HttpDelete("{id:long}/discounts")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveDiscount(long id, CancellationToken ct)
    {
        await _invoiceService.RemoveDiscountAsync(id, User.GetUserId(), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Discount removed"));
    }
}
