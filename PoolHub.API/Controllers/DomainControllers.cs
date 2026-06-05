using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Booking;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Invoice;
using PoolHub.Core.DTOs.Order;
using PoolHub.Core.DTOs.Product;
using PoolHub.Core.DTOs.Session;
using PoolHub.Core.DTOs.Venue;
using PoolHub.Core.Interfaces;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/floors")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
public class FloorsController(ICrudService s) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] PaginationRequest r, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetFloorsAsync(r, ct)));
    [HttpGet("{id:int}")] public async Task<ActionResult<ApiResponse<object>>> GetById(int id, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetFloorAsync(id, ct)));
    [HttpPost] public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] FloorDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.CreateFloorAsync(dto, ct)));
    [HttpPut("{id:int}")] public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] FloorDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.UpdateFloorAsync(id, dto, ct)));
    [HttpDelete("{id:int}")] public async Task<ActionResult<ApiResponse<object>>> Delete(int id, CancellationToken ct) { await s.DeleteFloorAsync(id, ct); return Ok(ApiResponse<object>.Ok(new { }, "Deleted")); }
}

[ApiController]
[Route("api/zones")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
public class ZonesController(ICrudService s) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] PaginationRequest r, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetZonesAsync(r, ct)));
    [HttpGet("{id:int}")] public async Task<ActionResult<ApiResponse<object>>> GetById(int id, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetZoneAsync(id, ct)));
    [HttpPost] public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] ZoneDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.CreateZoneAsync(dto, ct)));
    [HttpPut("{id:int}")] public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] ZoneDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.UpdateZoneAsync(id, dto, ct)));
    [HttpDelete("{id:int}")] public async Task<ActionResult<ApiResponse<object>>> Delete(int id, CancellationToken ct) { await s.DeleteZoneAsync(id, ct); return Ok(ApiResponse<object>.Ok(new { }, "Deleted")); }
}

[ApiController]
[Route("api/table-types")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
public class TableTypesController(ICrudService s) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] PaginationRequest r, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetTableTypesAsync(r, ct)));
    [HttpGet("{id:int}")] public async Task<ActionResult<ApiResponse<object>>> GetById(int id, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetTableTypeAsync(id, ct)));
    [HttpPost] public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] TableTypeDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.CreateTableTypeAsync(dto, ct)));
    [HttpPut("{id:int}")] public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] TableTypeDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.UpdateTableTypeAsync(id, dto, ct)));
    [HttpDelete("{id:int}")] public async Task<ActionResult<ApiResponse<object>>> Delete(int id, CancellationToken ct) { await s.DeleteTableTypeAsync(id, ct); return Ok(ApiResponse<object>.Ok(new { }, "Deleted")); }
}

[ApiController]
[Route("api/venue-tables")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff + "," + RoleConstants.Cashier)]
public class VenueTablesController(ICrudService s) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] PaginationRequest r, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetVenueTablesAsync(r, ct)));
    [HttpGet("{id:int}")] public async Task<ActionResult<ApiResponse<object>>> GetById(int id, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetVenueTableAsync(id, ct)));
    [HttpPost] [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)] public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] VenueTableDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.CreateVenueTableAsync(dto, ct)));
    [HttpPut("{id:int}")] [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)] public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] VenueTableDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.UpdateVenueTableAsync(id, dto, ct)));
    [HttpDelete("{id:int}")] [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)] public async Task<ActionResult<ApiResponse<object>>> Delete(int id, CancellationToken ct) { await s.DeleteVenueTableAsync(id, ct); return Ok(ApiResponse<object>.Ok(new { }, "Deleted")); }
}

[ApiController]
[Route("api/pricing-plans")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
public class PricingPlansController(ICrudService s) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] PaginationRequest r, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetPricingPlansAsync(r, ct)));
    [HttpGet("rules")] public async Task<ActionResult<ApiResponse<object>>> GetRules([FromQuery] PaginationRequest r, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await s.GetPricingPlanRulesAsync(r, ct)));
}

[ApiController]
[Route("api/bookings")]
public class BookingsController(IBookingService bookingService, ICrudService crud) : ControllerBase
{
    [HttpGet] [Authorize] public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] PaginationRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await crud.GetBookingsCrudAsync(request, ct)));
    [HttpGet("{id:int}")] [Authorize] public async Task<ActionResult<ApiResponse<object>>> GetById(int id, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await crud.GetBookingAsync(id, ct)));
    [HttpPost] [AllowAnonymous] public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] CreateBookingRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await bookingService.CreateAsync(request, ct)));
    [HttpDelete("{id:int}")] [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)] public async Task<ActionResult<ApiResponse<object>>> Delete(int id, CancellationToken ct) { await crud.DeleteBookingAsync(id, ct); return Ok(ApiResponse<object>.Ok(new { }, "Deleted")); }
}

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController(IProductService productService, ICrudService crud) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] PaginationRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await productService.GetProductsAsync(request, ct)));
    [HttpGet("{id:int}")] public async Task<ActionResult<ApiResponse<object>>> GetById(int id, CancellationToken ct) { var p = await productService.GetProductsAsync(new PaginationRequest{PageNumber=1,PageSize=1000}, ct); var item = p.Items.FirstOrDefault(x => x.ProductId == id); if (item is null) return NotFound(ApiResponse<object>.Fail("Product not found.")); return Ok(ApiResponse<object>.Ok(item)); }
    [HttpGet("categories")] public async Task<ActionResult<ApiResponse<object>>> Categories([FromQuery] PaginationRequest r, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await crud.GetProductCategoriesAsync(r, ct)));
    [HttpPost("categories")] [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)] public async Task<ActionResult<ApiResponse<object>>> CreateCategory([FromBody] ProductCategoryDto dto, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await crud.CreateProductCategoryAsync(dto, ct)));
    [HttpPost] [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)] public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] CreateProductRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await productService.CreateProductAsync(request, ct)));
}

[ApiController]
[Route("api/sessions")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff + "," + RoleConstants.Cashier)]
public class SessionsController(ISessionService sessionService) : ControllerBase
{
    [HttpPost("start")] public async Task<ActionResult<ApiResponse<object>>> Start([FromBody] StartSessionRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await sessionService.StartAsync(User.GetUserId(), request, ct)));
    [HttpPost("{sessionId:long}/end")] public async Task<ActionResult<ApiResponse<object>>> Close(long sessionId, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await sessionService.CloseAsync(sessionId, User.GetUserId(), ct)));
    [HttpPost("{sessionId:long}/switch")] public async Task<ActionResult<ApiResponse<object>>> Transfer(long sessionId, [FromBody] TransferTableRequest request, CancellationToken ct) { await sessionService.TransferTableAsync(sessionId, request.NewTableId, User.GetUserId(), ct); return Ok(ApiResponse<object>.Ok(new { }, "Switched")); }
}

[ApiController]
[Route("api/orders")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff + "," + RoleConstants.Cashier)]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpPost] public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] CreateOrderRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await orderService.CreateOrderAsync(User.GetUserId(), request, ct)));
    [HttpPost("{orderId:int}/items")] public async Task<ActionResult<ApiResponse<object>>> AddItem(int orderId, [FromBody] AddOrderItemRequest request, CancellationToken ct) { await orderService.AddOrderItemAsync(orderId, request, ct); return Ok(ApiResponse<object>.Ok(new { }, "Item added")); }
}

[ApiController]
[Route("api/invoices")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff + "," + RoleConstants.Cashier)]
public class InvoicesController(IInvoiceService invoiceService) : ControllerBase
{
    [HttpPost("generate/{sessionId:long}")] public async Task<ActionResult<ApiResponse<object>>> Generate(long sessionId, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await invoiceService.GenerateFromSessionAsync(sessionId, User.GetUserId(), ct)));
    [HttpPost("payments")] public async Task<ActionResult<ApiResponse<object>>> Payment([FromBody] CreatePaymentRequest request, CancellationToken ct) { await invoiceService.CreatePaymentAsync(request, User.GetUserId(), ct); return Ok(ApiResponse<object>.Ok(new { }, "Payment created")); }
}

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase { [HttpGet] public ActionResult<ApiResponse<object>> Get() => Ok(ApiResponse<object>.Ok(new[] { new { id = 1, title = "Demo notification" } })); }

[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
public class AuditLogsController(IAuditService auditService) : ControllerBase
{ [HttpGet] public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] PaginationRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await auditService.GetAuditLogsAsync(request, ct))); }
