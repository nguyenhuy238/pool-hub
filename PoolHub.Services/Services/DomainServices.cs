using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Booking;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Invoice;
using PoolHub.Core.DTOs.Order;
using PoolHub.Core.DTOs.Product;
using PoolHub.Core.DTOs.Session;
using PoolHub.Core.DTOs.Venue;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared;
using PoolHub.Shared.Exceptions;

namespace PoolHub.Services.Services;

public class VenueService(PoolHubDbContext db) : IVenueService
{
    public async Task<IEnumerable<FloorDto>> GetFloorsAsync(CancellationToken ct) => await db.Floors.Select(x => new FloorDto { FloorId = x.FloorId, Name = x.Name, IsActive = x.IsActive }).ToListAsync(ct);
    public async Task<IEnumerable<ZoneDto>> GetZonesAsync(CancellationToken ct) => await db.Zones.Select(x => new ZoneDto { ZoneId = x.ZoneId, FloorId = x.FloorId, Name = x.Name, IsActive = x.IsActive }).ToListAsync(ct);
    public async Task<IEnumerable<TableTypeDto>> GetTableTypesAsync(CancellationToken ct) => await db.TableTypes.Select(x => new TableTypeDto { TableTypeId = x.TableTypeId, Name = x.Name, Code = x.Code, DefaultCapacity = x.DefaultCapacity }).ToListAsync(ct);
    public async Task<IEnumerable<VenueTableDto>> GetTablesAsync(CancellationToken ct) => await db.VenueTables.Select(x => new VenueTableDto { TableId = x.TableId, ZoneId = x.ZoneId, TableTypeId = x.TableTypeId, TableCode = x.TableCode, TableName = x.TableName, Capacity = x.Capacity, OperationalStatus = x.OperationalStatus }).ToListAsync(ct);
    public async Task<IEnumerable<PricingPlanDto>> GetPricingPlansAsync(CancellationToken ct) => await db.PricingPlans.Select(x => new PricingPlanDto { PricingPlanId = x.PricingPlanId, Name = x.Name, IsDefault = x.IsDefault, IsActive = x.IsActive }).ToListAsync(ct);
    public async Task<IEnumerable<PricingPlanRuleDto>> GetPricingPlanRulesAsync(CancellationToken ct) => await db.PricingPlanRules.Select(x => new PricingPlanRuleDto { PricingPlanRuleId = x.PricingPlanRuleId, PricingPlanId = x.PricingPlanId, TableTypeId = x.TableTypeId, DayOfWeek = x.DayOfWeek, HourlyRate = x.HourlyRate }).ToListAsync(ct);
}

public class BookingService(PoolHubDbContext db) : IBookingService
{
    public async Task<PagedResult<BookingDto>> GetBookingsAsync(PaginationRequest request, CancellationToken ct)
    {
        var query = db.Bookings.AsQueryable();
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.BookingId).Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).Select(x => new BookingDto { BookingId = x.BookingId, BookingCode = x.BookingCode, CustomerId = x.CustomerId, TableId = x.TableId, StartTimeUtc = x.StartTimeUtc, EndTimeUtc = x.EndTimeUtc, Status = x.Status }).ToListAsync(ct);
        return new PagedResult<BookingDto> { Items = items, PageNumber = request.PageNumber, PageSize = request.PageSize, TotalCount = total };
    }

    public async Task<BookingDto> CreateAsync(CreateBookingRequest request, CancellationToken ct)
    {
        var entity = new Booking
        {
            CustomerId = request.CustomerId,
            TableId = request.TableId,
            TableTypeId = request.TableTypeId,
            BookingCode = $"BK{DateTime.UtcNow:yyyyMMddHHmmss}",
            StartTimeUtc = request.StartTimeUtc,
            EndTimeUtc = request.EndTimeUtc,
            NumberOfGuests = request.NumberOfGuests,
            Status = 1
        };
        db.Bookings.Add(entity);
        await db.SaveChangesAsync(ct);
        return new BookingDto { BookingId = entity.BookingId, BookingCode = entity.BookingCode, CustomerId = entity.CustomerId, TableId = entity.TableId, StartTimeUtc = entity.StartTimeUtc, EndTimeUtc = entity.EndTimeUtc, Status = entity.Status };
    }
}

public class ProductService(PoolHubDbContext db) : IProductService
{
    public async Task<IEnumerable<ProductCategoryDto>> GetCategoriesAsync(CancellationToken ct) => await db.ProductCategories.Select(x => new ProductCategoryDto { ProductCategoryId = x.ProductCategoryId, Name = x.Name }).ToListAsync(ct);

    public async Task<PagedResult<ProductDto>> GetProductsAsync(PaginationRequest request, CancellationToken ct)
    {
        var query = db.Products.AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search)) query = query.Where(x => x.Name.Contains(request.Search) || x.Sku.Contains(request.Search));
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.ProductId).Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).Select(x => new ProductDto { ProductId = x.ProductId, ProductCategoryId = x.ProductCategoryId, Name = x.Name, Sku = x.Sku, UnitPrice = x.UnitPrice, StockQuantity = x.StockQuantity }).ToListAsync(ct);
        return new PagedResult<ProductDto> { Items = items, PageNumber = request.PageNumber, PageSize = request.PageSize, TotalCount = total };
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken ct)
    {
        if (await db.Products.AnyAsync(x => x.Sku == request.Sku, ct)) throw new ConflictException("Product sku already exists.");
        var product = new Product { ProductCategoryId = request.ProductCategoryId, Name = request.Name, Sku = request.Sku, UnitPrice = request.UnitPrice, StockQuantity = request.StockQuantity, IsActive = true };
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return new ProductDto { ProductId = product.ProductId, ProductCategoryId = product.ProductCategoryId, Name = product.Name, Sku = product.Sku, UnitPrice = product.UnitPrice, StockQuantity = product.StockQuantity };
    }
}

public class SessionService(PoolHubDbContext db) : ISessionService
{
    public async Task<SessionDto> StartAsync(long userId, StartSessionRequest request, CancellationToken ct)
    {
        var session = new Session
        {
            SessionCode = $"SS{DateTime.UtcNow:yyyyMMddHHmmss}",
            BookingId = request.BookingId,
            CustomerId = request.CustomerId,
            OpenedByUserId = userId,
            StartedAtUtc = DateTime.UtcNow,
            Status = 1
        };
        db.Sessions.Add(session);
        await db.SaveChangesAsync(ct);

        db.SessionTableAssignments.Add(new SessionTableAssignment { SessionId = session.SessionId, TableId = request.TableId, StartedAtUtc = DateTime.UtcNow, AssignedByUserId = userId });
        await db.SaveChangesAsync(ct);
        return new SessionDto { SessionId = session.SessionId, SessionCode = session.SessionCode, StartedAtUtc = session.StartedAtUtc, EndedAtUtc = session.EndedAtUtc, Status = session.Status };
    }

    public async Task<SessionDto> CloseAsync(long sessionId, long? closedByUserId, CancellationToken ct)
    {
        var session = await db.Sessions.FindAsync([sessionId], ct) ?? throw new NotFoundException("Session not found.");
        session.Status = 2;
        session.EndedAtUtc = DateTime.UtcNow;
        session.ClosedByUserId = closedByUserId;
        await db.SaveChangesAsync(ct);
        return new SessionDto { SessionId = session.SessionId, SessionCode = session.SessionCode, StartedAtUtc = session.StartedAtUtc, EndedAtUtc = session.EndedAtUtc, Status = session.Status };
    }

    public async Task TransferTableAsync(long sessionId, long newTableId, long? assignedByUserId, CancellationToken ct)
    {
        var current = await db.SessionTableAssignments.Where(x => x.SessionId == sessionId && x.EndedAtUtc == null).OrderByDescending(x => x.StartedAtUtc).FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Active assignment not found.");
        current.EndedAtUtc = DateTime.UtcNow;
        db.SessionTableAssignments.Add(new SessionTableAssignment { SessionId = sessionId, TableId = newTableId, StartedAtUtc = DateTime.UtcNow, AssignedByUserId = assignedByUserId });
        await db.SaveChangesAsync(ct);
    }
}

public class OrderService(PoolHubDbContext db) : IOrderService
{
    public async Task<OrderDto> CreateOrderAsync(long userId, CreateOrderRequest request, CancellationToken ct)
    {
        var order = new Order { SessionId = request.SessionId, OrderedByUserId = userId, OrderCode = $"OD{DateTime.UtcNow:yyyyMMddHHmmss}", Status = 1 };
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
        return new OrderDto { OrderId = order.OrderId, SessionId = order.SessionId, Status = order.Status };
    }

    public async Task AddOrderItemAsync(long orderId, AddOrderItemRequest request, CancellationToken ct)
    {
        var order = await db.Orders.FindAsync([orderId], ct) ?? throw new NotFoundException("Order not found.");
        var product = await db.Products.FindAsync([request.ProductId], ct) ?? throw new NotFoundException("Product not found.");
        if (product.IsStockTracked && product.StockQuantity < request.Quantity) throw new BusinessRuleException("Not enough stock quantity.");

        var lineTotal = product.UnitPrice * request.Quantity;
        var item = new OrderItem { OrderId = orderId, ProductId = request.ProductId, ProductNameSnapshot = product.Name, Quantity = request.Quantity, UnitPriceSnapshot = product.UnitPrice, LineTotalAmount = lineTotal };
        order.SubtotalAmount += lineTotal;
        if (product.IsStockTracked) product.StockQuantity -= request.Quantity;
        db.OrderItems.Add(item);
        db.InventoryTransactions.Add(new InventoryTransaction { ProductId = request.ProductId, TransactionType = 4, Quantity = -request.Quantity, ReferenceType = "ORDER", ReferenceId = orderId, Note = "ORDER", CreatedByUserId = order.OrderedByUserId });
        await db.SaveChangesAsync(ct);
    }
}

public class InvoiceService(PoolHubDbContext db) : IInvoiceService
{
    public async Task<InvoiceDto> GenerateFromSessionAsync(long sessionId, long? issuedByUserId, CancellationToken ct)
    {
        var exists = await db.Invoices.FirstOrDefaultAsync(x => x.SessionId == sessionId, ct);
        if (exists is not null) return new InvoiceDto { InvoiceId = exists.InvoiceId, SessionId = exists.SessionId, InvoiceCode = exists.InvoiceCode, GrandTotalAmount = exists.GrandTotalAmount };

        var session = await db.Sessions.FindAsync([sessionId], ct) ?? throw new NotFoundException("Session not found.");
        var productTotal = await db.Orders.Where(o => o.SessionId == sessionId).SumAsync(o => o.SubtotalAmount, ct);
        var timeTotal = await db.SessionTableAssignments.Where(x => x.SessionId == sessionId).SumAsync(x => x.Amount ?? 0, ct);
        var subtotal = productTotal + timeTotal;

        var invoice = new Invoice
        {
            SessionId = sessionId,
            CustomerId = session.CustomerId,
            InvoiceCode = $"INV{DateTime.UtcNow:yyyyMMddHHmmss}",
            TimeSubtotalAmount = timeTotal,
            ProductSubtotalAmount = productTotal,
            SubtotalAmount = subtotal,
            DiscountAmount = 0,
            TaxAmount = 0,
            GrandTotalAmount = subtotal,
            PaidAmount = 0,
            PaymentStatus = 1,
            Status = 1,
            IssuedByUserId = issuedByUserId,
            IssuedAtUtc = DateTime.UtcNow
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct);
        return new InvoiceDto { InvoiceId = invoice.InvoiceId, SessionId = invoice.SessionId, InvoiceCode = invoice.InvoiceCode, GrandTotalAmount = invoice.GrandTotalAmount };
    }

    public async Task CreatePaymentAsync(CreatePaymentRequest request, long? receivedByUserId, CancellationToken ct)
    {
        var payment = new Payment { InvoiceId = request.InvoiceId, PaymentMethodId = request.PaymentMethodId, Amount = request.Amount, PaymentStatus = 1, ReceivedByUserId = receivedByUserId, PaidAtUtc = DateTime.UtcNow };
        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);
    }
}

public class AuditService(PoolHubDbContext db) : IAuditService
{
    public async Task<PagedResult<object>> GetAuditLogsAsync(PaginationRequest request, CancellationToken ct)
    {
        var query = db.AuditLogs.AsQueryable();
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.AuditLogId).Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).Select(x => (object)new { x.AuditLogId, x.ActorUserId, x.Action, x.EntityName, x.EntityId, x.EntityPublicId, x.CreatedAtUtc }).ToListAsync(ct);
        return new PagedResult<object> { Items = items, PageNumber = request.PageNumber, PageSize = request.PageSize, TotalCount = total };
    }
}
