using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Admin;
using PoolHub.Core.DTOs.Invoice;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Extensions;
using System.Text;

namespace PoolHub.API.Controllers;

[ApiController, Route("api/discounts")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Cashier, Policy = PermissionConstants.DiscountsManage)]
public class DiscountsController(IAdminManagementService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] DiscountQueryRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await service.GetDiscountsAsync(request, ct)));
    [HttpPost, Authorize(Roles = RoleConstants.Admin)] public async Task<ActionResult<ApiResponse<object>>> Create(UpsertDiscountRequest request, CancellationToken ct) => StatusCode(201, ApiResponse<object>.Ok(await service.CreateDiscountAsync(request, User.GetUserId(), ct)));
    [HttpPut("{id:long}"), Authorize(Roles = RoleConstants.Admin)] public async Task<ActionResult<ApiResponse<object>>> Update(long id, UpsertDiscountRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await service.UpdateDiscountAsync(id, request, User.GetUserId(), ct)));
    [HttpPatch("{id:long}/status"), Authorize(Roles = RoleConstants.Admin)] public async Task<ActionResult<ApiResponse<object>>> Status(long id, UpdateActiveStatusRequest request, CancellationToken ct) { await service.SetDiscountStatusAsync(id, request.IsActive, User.GetUserId(), ct); return Ok(ApiResponse<object>.Ok(new { }, "Discount status updated.")); }
    [HttpPost("validate")] public async Task<ActionResult<ApiResponse<object>>> Validate(ValidateDiscountRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await service.ValidateDiscountAsync(request, ct)));
}

[ApiController, Route("api/inventory-transactions")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager, Policy = PermissionConstants.InventoryManage)]
public class InventoryTransactionsController(IAdminManagementService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] InventoryQueryRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await service.GetInventoryAsync(request, ct)));
    [HttpPost("stock-adjust")] public async Task<ActionResult<ApiResponse<object>>> Adjust(StockAdjustRequest request, CancellationToken ct) => StatusCode(201, ApiResponse<object>.Ok(await service.AdjustStockAsync(request, User.GetUserId(), ct)));
    [HttpGet("low-stock")] public async Task<ActionResult<ApiResponse<object>>> LowStock(CancellationToken ct) => Ok(ApiResponse<object>.Ok(await service.GetLowStockAsync(ct)));
}

[ApiController, Route("api/payment-methods")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Cashier, Policy = PermissionConstants.PaymentsManage)]
public class PaymentMethodsController(IAdminManagementService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<object>>> Get(CancellationToken ct) => Ok(ApiResponse<object>.Ok(await service.GetAllPaymentMethodsAsync(ct)));
    [HttpPost, Authorize(Roles = RoleConstants.Admin)] public async Task<ActionResult<ApiResponse<object>>> Create(UpsertPaymentMethodRequest request, CancellationToken ct) => StatusCode(201, ApiResponse<object>.Ok(await service.CreatePaymentMethodAsync(request, User.GetUserId(), ct)));
    [HttpPut("{id:long}"), Authorize(Roles = RoleConstants.Admin)] public async Task<ActionResult<ApiResponse<object>>> Update(long id, UpsertPaymentMethodRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await service.UpdatePaymentMethodAsync(id, request, User.GetUserId(), ct)));
    [HttpPatch("{id:long}/status"), Authorize(Roles = RoleConstants.Admin)] public async Task<ActionResult<ApiResponse<object>>> Status(long id, UpdateActiveStatusRequest request, CancellationToken ct) { await service.SetPaymentMethodStatusAsync(id, request.IsActive, User.GetUserId(), ct); return Ok(ApiResponse<object>.Ok(new { }, "Payment method status updated.")); }
}

[ApiController, Route("api/payments")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Cashier, Policy = PermissionConstants.PaymentsManage)]
public class PaymentsController(IAdminManagementService admin, IInvoiceService invoices) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] PaymentQueryRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await admin.GetPaymentsAsync(request, ct)));
    [HttpPost] public async Task<IActionResult> Create(CreatePaymentRequest request, CancellationToken ct) { await invoices.CreatePaymentAsync(request, User.GetUserId(), ct); return StatusCode(201, ApiResponse<object>.Ok(new { }, "Payment recorded.")); }
    [HttpPost("{id:long}/refund")] public async Task<IActionResult> Refund(long id, [FromBody] RefundPaymentRequest request, CancellationToken ct) { await invoices.RefundPaymentAsync(id, request.Reason, User.GetUserId(), ct); return Ok(ApiResponse<object>.Ok(new { }, "Payment refunded.")); }
}

[ApiController, Route("api/reports")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager, Policy = PermissionConstants.ReportsView)]
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
