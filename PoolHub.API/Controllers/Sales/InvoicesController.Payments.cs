using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Invoice;
using PoolHub.Shared;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers;

public partial class InvoicesController
{
    [HttpPost("payments")]
    public async Task<ActionResult<ApiResponse<CreatePaymentResponse>>> Payment([FromBody] CreatePaymentRequest request, CancellationToken ct)
    {
        var response = await _invoiceService.CreatePaymentAsync(request, User.GetUserId(), ct);
        return Ok(ApiResponse<CreatePaymentResponse>.Ok(response, "Payment created"));
    }
}
