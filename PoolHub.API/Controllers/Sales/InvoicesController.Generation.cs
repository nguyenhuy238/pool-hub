using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Invoice;
using PoolHub.Shared;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers;

public partial class InvoicesController
{
    [HttpPost("generate/{sessionId:long}")]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> Generate(long sessionId, CancellationToken ct) =>
        Ok(ApiResponse<InvoiceDto>.Ok(await _invoiceService.GenerateFromSessionAsync(sessionId, User.GetUserId(), ct)));
}
