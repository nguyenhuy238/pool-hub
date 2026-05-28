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
    public async Task<IEnumerable<ProductCategoryDto>> GetCategoriesAsync(CancellationToken ct) => await db.ProductCategories.Select(x => new ProductCategoryDto { CategoryId = x.CategoryId, Name = x.Name, Code = x.Code }).ToListAsync(ct);

    public async Task<PagedResult<ProductDto>> GetProductsAsync(PaginationRequest request, CancellationToken ct)
    {
        var query = db.Products.AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search)) query = query.Where(x => x.Name.Contains(request.Search) || x.Code.Contains(request.Search));
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.ProductId).Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).Select(x => new ProductDto { ProductId = x.ProductId, CategoryId = x.CategoryId, Name = x.Name, Code = x.Code, UnitPrice = x.UnitPrice, StockQuantity = x.StockQuantity }).ToListAsync(ct);
        return new PagedResult<ProductDto> { Items = items, PageNumber = request.PageNumber, PageSize = request.PageSize, TotalCount = total };
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken ct)
    {
        if (await db.Products.AnyAsync(x => x.Code == request.Code, ct)) throw new ConflictException("Product code already exists.");
        var product = new Product { CategoryId = request.CategoryId, Name = request.Name, Code = request.Code, UnitPrice = request.UnitPrice, StockQuantity = request.StockQuantity, IsActive = true };
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return new ProductDto { ProductId = product.ProductId, CategoryId = product.CategoryId, Name = product.Name, Code = product.Code, UnitPrice = product.UnitPrice, StockQuantity = product.StockQuantity };
    }
}

public class SessionService(PoolHubDbContext db) : ISessionService
{
    public async Task<SessionDto> StartAsync(int userId, StartSessionRequest request, CancellationToken ct)
    {
        var session = new Session { SessionCode = $"SE{DateTime.UtcNow:yyyyMMddHHmmss}", BookingId = request.BookingId, StartedByUserId = userId, StartTimeUtc = DateTime.UtcNow, Status = 1 };
        db.Sessions.Add(session);
        await db.SaveChangesAsync(ct);
        db.SessionTableAssignments.Add(new SessionTableAssignment { SessionId = session.SessionId, TableId = request.TableId, AssignedAtUtc = DateTime.UtcNow, IsPrimary = true });
        await db.SaveChangesAsync(ct);
        return new SessionDto { SessionId = session.SessionId, SessionCode = session.SessionCode, StartTimeUtc = session.StartTimeUtc, EndTimeUtc = session.EndTimeUtc, Status = session.Status };
    }

    public async Task<SessionDto> CloseAsync(int sessionId, CancellationToken ct)
    {
        var session = await db.Sessions.FindAsync([sessionId], ct) ?? throw new NotFoundException("Session not found.");
        session.Status = 2;
        session.EndTimeUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new SessionDto { SessionId = session.SessionId, SessionCode = session.SessionCode, StartTimeUtc = session.StartTimeUtc, EndTimeUtc = session.EndTimeUtc, Status = session.Status };
    }

    public async Task TransferTableAsync(int sessionId, int newTableId, CancellationToken ct)
    {
        var current = await db.SessionTableAssignments.Where(x => x.SessionId == sessionId && x.ReleasedAtUtc == null).OrderByDescending(x => x.AssignedAtUtc).FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Active assignment not found.");
        current.ReleasedAtUtc = DateTime.UtcNow;
        db.SessionTableAssignments.Add(new SessionTableAssignment { SessionId = sessionId, TableId = newTableId, AssignedAtUtc = DateTime.UtcNow, IsPrimary = true });
        await db.SaveChangesAsync(ct);
    }
}

public class OrderService(PoolHubDbContext db) : IOrderService
{
    public async Task<OrderDto> CreateOrderAsync(int userId, CreateOrderRequest request, CancellationToken ct)
    {
        var order = new Order { SessionId = request.SessionId, CreatedByUserId = userId, Status = 1 };
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
        return new OrderDto { OrderId = order.OrderId, SessionId = order.SessionId, Status = order.Status };
    }

    public async Task AddOrderItemAsync(int orderId, AddOrderItemRequest request, CancellationToken ct)
    {
        var product = await db.Products.FindAsync([request.ProductId], ct) ?? throw new NotFoundException("Product not found.");
        if (product.StockQuantity < request.Quantity) throw new BusinessRuleException("Not enough stock quantity.");
        var item = new OrderItem { OrderId = orderId, ProductId = request.ProductId, Quantity = request.Quantity, UnitPrice = product.UnitPrice, LineTotal = product.UnitPrice * request.Quantity };
        product.StockQuantity -= request.Quantity;
        db.OrderItems.Add(item);
        db.InventoryTransactions.Add(new InventoryTransaction { ProductId = request.ProductId, QuantityChange = -request.Quantity, StockAfter = product.StockQuantity, Reason = "ORDER" });
        await db.SaveChangesAsync(ct);
    }
}

public class InvoiceService(PoolHubDbContext db) : IInvoiceService
{
    public async Task<InvoiceDto> GenerateFromSessionAsync(int sessionId, CancellationToken ct)
    {
        var exists = await db.Invoices.FirstOrDefaultAsync(x => x.SessionId == sessionId, ct);
        if (exists is not null) return new InvoiceDto { InvoiceId = exists.InvoiceId, SessionId = exists.SessionId, InvoiceCode = exists.InvoiceCode, FinalAmount = exists.FinalAmount };

        var total = await (from o in db.Orders where o.SessionId == sessionId
                           join i in db.OrderItems on o.OrderId equals i.OrderId
                           select i.LineTotal).SumAsync(ct);

        var invoice = new Invoice { SessionId = sessionId, InvoiceCode = $"INV{DateTime.UtcNow:yyyyMMddHHmmss}", SubtotalAmount = total, DiscountAmount = 0, FinalAmount = total, Status = 1 };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct);
        return new InvoiceDto { InvoiceId = invoice.InvoiceId, SessionId = invoice.SessionId, InvoiceCode = invoice.InvoiceCode, FinalAmount = invoice.FinalAmount };
    }

    public async Task CreatePaymentAsync(CreatePaymentRequest request, CancellationToken ct)
    {
        var payment = new Payment { InvoiceId = request.InvoiceId, PaymentMethodId = request.PaymentMethodId, Amount = request.Amount, Status = 1 };
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
        var items = await query.OrderByDescending(x => x.AuditLogId).Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).Select(x => (object)new { x.AuditLogId, x.UserId, x.Action, x.EntityName, x.EntityId, x.CreatedAtUtc }).ToListAsync(ct);
        return new PagedResult<object> { Items = items, PageNumber = request.PageNumber, PageSize = request.PageSize, TotalCount = total };
    }
}
