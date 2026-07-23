using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Shared;

namespace PoolHub.API.Controllers;

public partial class InvoicesController
{
    [HttpGet("{id:long}/export-pdf")]
    public async Task<ActionResult<ApiResponse<object>>> ExportPdf(long id, CancellationToken ct)
    {
        var url = await _invoiceService.ExportPdfAsync(id, ct);
        return Ok(ApiResponse<object>.Ok(new { url }, "Exported successfully"));
    }

    [HttpGet("{id:long}/qr-code")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> GetQrCode(long id, CancellationToken ct)
    {
        var url = await _invoiceService.GetVietQrUrlAsync(id, ct);
        return Redirect(url);
    }
}
