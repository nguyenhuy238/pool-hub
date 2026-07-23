using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Admin;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using System.Text;

namespace PoolHub.API.Controllers;

[ApiController, Route("api/reports")]
[Authorize(Policy = PermissionConstants.ReportsView)]
public class ReportsController(IAdminManagementService service) : ControllerBase
{
    [HttpGet("revenue")] public async Task<ActionResult<ApiResponse<object>>> Revenue([FromQuery] ReportQueryRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await service.GetRevenueReportAsync(request, ct)));
    [HttpGet("table-usage")] public async Task<ActionResult<ApiResponse<object>>> TableUsage([FromQuery] ReportQueryRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await service.GetTableUsageReportAsync(request, ct)));
    [HttpGet("products")] public async Task<ActionResult<ApiResponse<object>>> Products([FromQuery] ReportQueryRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await service.GetProductReportAsync(request, ct)));
    [HttpGet("bookings")] public async Task<ActionResult<ApiResponse<object>>> Bookings([FromQuery] ReportQueryRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await service.GetBookingReportAsync(request, ct)));
    [HttpGet("customers")] public async Task<ActionResult<ApiResponse<object>>> Customers([FromQuery] ReportQueryRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await service.GetCustomerReportAsync(request, ct)));
    [HttpGet("payment-methods")] public async Task<ActionResult<ApiResponse<object>>> PaymentMethods([FromQuery] ReportQueryRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await service.GetPaymentMethodReportAsync(request, ct)));
    [HttpGet("inventory")] public async Task<ActionResult<ApiResponse<object>>> Inventory([FromQuery] ReportQueryRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await service.GetInventoryReportAsync(request, ct)));

    [HttpGet("revenue/export")]
    public async Task<IActionResult> ExportRevenue([FromQuery] ReportQueryRequest request, CancellationToken ct)
    {
        var rows = await service.GetRevenueReportAsync(request, ct);
        var csv = new StringBuilder("Date,Revenue,InvoiceCount\r\n");
        foreach (var row in rows) csv.AppendLine($"{row.Date:yyyy-MM-dd},{row.Revenue},{row.InvoiceCount}");
        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(),
            "text/csv", $"poolhub-revenue-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }
}
