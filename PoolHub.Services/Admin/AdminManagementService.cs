using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Admin;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Invoice;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Exceptions;

namespace PoolHub.Services.Admin;

public class AdminManagementService(PoolHubDbContext db, IAuditService audit) : IAdminManagementService
{
    public async Task<PagedResult<DiscountDto>> GetDiscountsAsync(DiscountQueryRequest request, CancellationToken ct)
    {
        Normalize(request);
        var query = db.Discounts.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(x => x.DiscountCode.Contains(request.Search) || x.Name.Contains(request.Search));
        if (request.IsActive.HasValue) query = query.Where(x => x.IsActive == request.IsActive);
        if (!string.IsNullOrWhiteSpace(request.DiscountType)) query = query.Where(x => x.DiscountType == request.DiscountType);
        if (!string.IsNullOrWhiteSpace(request.AppliesTo)) query = query.Where(x => x.AppliesTo == request.AppliesTo);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.DiscountId)
            .Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new DiscountDto {
                DiscountId = x.DiscountId, DiscountCode = x.DiscountCode, Name = x.Name, DiscountType = x.DiscountType,
                Value = x.Value, MaxAmount = x.MaxAmount, MinTimeSubtotal = x.MinTimeSubtotal, AppliesTo = x.AppliesTo,
                StartsAtUtc = x.StartsAtUtc, EndsAtUtc = x.EndsAtUtc, IsActive = x.IsActive
            }).ToListAsync(ct);
        return Page(items, request.PageNumber, request.PageSize, total);
    }

    public async Task<DiscountDto> CreateDiscountAsync(UpsertDiscountRequest request, long actorUserId, CancellationToken ct)
    {
        ValidateDiscountRequest(request);
        var code = request.DiscountCode.Trim().ToUpperInvariant();
        if (await db.Discounts.AnyAsync(x => x.DiscountCode == code, ct))
            throw new ConflictException("Discount code already exists.");
        var entity = new Discount { DiscountCode = code };
        Apply(entity, request);
        db.Discounts.Add(entity);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorUserId, "DISCOUNT_CREATED", nameof(Discount), entity.DiscountId, newValues: MapDiscount(entity), ct: ct);
        return MapDiscount(entity);
    }

    public async Task<DiscountDto> UpdateDiscountAsync(long id, UpsertDiscountRequest request, long actorUserId, CancellationToken ct)
    {
        ValidateDiscountRequest(request);
        var entity = await db.Discounts.FindAsync([id], ct) ?? throw new NotFoundException("Discount not found.");
        var old = MapDiscount(entity);
        var code = request.DiscountCode.Trim().ToUpperInvariant();
        if (await db.Discounts.AnyAsync(x => x.DiscountId != id && x.DiscountCode == code, ct))
            throw new ConflictException("Discount code already exists.");
        entity.DiscountCode = code;
        Apply(entity, request);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorUserId, "DISCOUNT_UPDATED", nameof(Discount), id, oldValues: old, newValues: MapDiscount(entity), ct: ct);
        return MapDiscount(entity);
    }

    public async Task SetDiscountStatusAsync(long id, bool isActive, long actorUserId, CancellationToken ct)
    {
        var entity = await db.Discounts.FindAsync([id], ct) ?? throw new NotFoundException("Discount not found.");
        entity.IsActive = isActive;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorUserId, "DISCOUNT_STATUS_CHANGED", nameof(Discount), id, newValues: new { isActive }, ct: ct);
    }

    public async Task<DiscountValidationDto> ValidateDiscountAsync(ValidateDiscountRequest request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var code = request.DiscountCode.Trim().ToUpperInvariant();
        var discount = await db.Discounts.AsNoTracking().FirstOrDefaultAsync(x =>
            x.DiscountCode == code && x.IsActive && x.StartsAtUtc <= now && (x.EndsAtUtc == null || x.EndsAtUtc >= now), ct);
        if (discount is null) return new() { IsValid = false, Message = "Discount is inactive, expired, or not found." };
        if (discount.MinTimeSubtotal.HasValue && request.TimeSubtotal < discount.MinTimeSubtotal)
            return new() { IsValid = false, Message = "Minimum time subtotal is not met." };
            
        var baseAmount = request.TimeSubtotal; // Preview only with time subtotal
        var amount = discount.DiscountType.Equals(DiscountTypes.Percentage, StringComparison.OrdinalIgnoreCase)
            ? baseAmount * discount.Value / 100m : discount.Value;
        if (discount.MaxAmount.HasValue) amount = Math.Min(amount, discount.MaxAmount.Value);
        amount = Math.Min(amount, baseAmount);
        return new() { IsValid = true, DiscountAmount = amount, Message = "Discount is valid for time charges." };
    }

    public async Task<PagedResult<InventoryTransactionDto>> GetInventoryAsync(InventoryQueryRequest request, CancellationToken ct)
    {
        Normalize(request);
        var query = from item in db.InventoryTransactions.AsNoTracking()
                    join product in db.Products.AsNoTracking() on item.ProductId equals product.ProductId
                    select new { item, product };
        if (request.ProductId.HasValue) query = query.Where(x => x.item.ProductId == request.ProductId);
        if (request.TransactionType.HasValue) query = query.Where(x => x.item.TransactionType == request.TransactionType);
        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(x => x.product.Name.Contains(request.Search) || x.product.Sku.Contains(request.Search));
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.item.InventoryTransactionId)
            .Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new InventoryTransactionDto {
                InventoryTransactionId = x.item.InventoryTransactionId, ProductId = x.item.ProductId,
                ProductName = x.product.Name, TransactionType = x.item.TransactionType, Quantity = x.item.Quantity,
                UnitCost = x.item.UnitCost, ReferenceType = x.item.ReferenceType, ReferenceId = x.item.ReferenceId,
                Note = x.item.Note, CreatedByUserId = x.item.CreatedByUserId, CreatedAtUtc = x.item.CreatedAtUtc
            }).ToListAsync(ct);
        return Page(items, request.PageNumber, request.PageSize, total);
    }

    public async Task<InventoryTransactionDto> AdjustStockAsync(StockAdjustRequest request, long actorUserId, CancellationToken ct)
    {
        var product = await db.Products.FindAsync([request.ProductId], ct) ?? throw new NotFoundException("Product not found.");
        if (!product.IsStockTracked) throw new BusinessRuleException("Stock is not tracked for this product.");
        var delta = request.TransactionType == InventoryTransactionTypes.Import ? request.Quantity : -request.Quantity;
        if (product.StockQuantity + delta < 0) throw new ConflictException("Insufficient stock.");
        product.StockQuantity += delta;
        var entity = new InventoryTransaction {
            ProductId = product.ProductId, TransactionType = request.TransactionType, Quantity = delta,
            UnitCost = request.UnitCost, ReferenceType = "ADMIN_ADJUSTMENT", Note = request.Note, CreatedByUserId = actorUserId
        };
        db.InventoryTransactions.Add(entity);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorUserId, "INVENTORY_ADJUSTED", nameof(Product), product.ProductId,
            newValues: new { product.StockQuantity, request.TransactionType, request.Quantity }, ct: ct);
        return new() {
            InventoryTransactionId = entity.InventoryTransactionId, ProductId = product.ProductId, ProductName = product.Name,
            TransactionType = entity.TransactionType, Quantity = entity.Quantity, UnitCost = entity.UnitCost,
            ReferenceType = entity.ReferenceType, Note = entity.Note, CreatedByUserId = actorUserId, CreatedAtUtc = entity.CreatedAtUtc
        };
    }

    public Task<List<LowStockProductDto>> GetLowStockAsync(CancellationToken ct) =>
        db.Products.AsNoTracking().Where(x => x.IsActive && x.IsStockTracked && x.LowStockThreshold.HasValue && x.StockQuantity <= x.LowStockThreshold)
            .OrderBy(x => x.StockQuantity).Select(x => new LowStockProductDto {
                ProductId = x.ProductId, Sku = x.Sku, Name = x.Name, StockQuantity = x.StockQuantity,
                LowStockThreshold = x.LowStockThreshold!.Value
            }).ToListAsync(ct);

    public Task<List<PaymentMethodDto>> GetAllPaymentMethodsAsync(CancellationToken ct) =>
        db.PaymentMethods.AsNoTracking().OrderBy(x => x.Name).Select(x => new PaymentMethodDto {
            PaymentMethodId = x.PaymentMethodId, Name = x.Name, Code = x.Code, Description = x.Description, IsActive = x.IsActive
        }).ToListAsync(ct);

    public async Task<PaymentMethodDto> CreatePaymentMethodAsync(UpsertPaymentMethodRequest request, long actorUserId, CancellationToken ct)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.PaymentMethods.AnyAsync(x => x.Code == code, ct)) throw new ConflictException("Payment method code already exists.");
        var entity = new PaymentMethod { Name = request.Name.Trim(), Code = code, Description = request.Description, IsActive = request.IsActive };
        db.PaymentMethods.Add(entity);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorUserId, "PAYMENT_METHOD_CREATED", nameof(PaymentMethod), entity.PaymentMethodId, newValues: entity, ct: ct);
        return MapPaymentMethod(entity);
    }

    public async Task<PaymentMethodDto> UpdatePaymentMethodAsync(long id, UpsertPaymentMethodRequest request, long actorUserId, CancellationToken ct)
    {
        var entity = await db.PaymentMethods.FindAsync([id], ct) ?? throw new NotFoundException("Payment method not found.");
        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.PaymentMethods.AnyAsync(x => x.PaymentMethodId != id && x.Code == code, ct)) throw new ConflictException("Payment method code already exists.");
        entity.Name = request.Name.Trim(); entity.Code = code; entity.Description = request.Description; entity.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorUserId, "PAYMENT_METHOD_UPDATED", nameof(PaymentMethod), id, newValues: entity, ct: ct);
        return MapPaymentMethod(entity);
    }

    public async Task SetPaymentMethodStatusAsync(long id, bool isActive, long actorUserId, CancellationToken ct)
    {
        var entity = await db.PaymentMethods.FindAsync([id], ct) ?? throw new NotFoundException("Payment method not found.");
        entity.IsActive = isActive;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorUserId, "PAYMENT_METHOD_STATUS_CHANGED", nameof(PaymentMethod), id, newValues: new { isActive }, ct: ct);
    }

    public async Task<PagedResult<PaymentDto>> GetPaymentsAsync(PaymentQueryRequest request, CancellationToken ct)
    {
        Normalize(request);
        var query = db.Payments.AsNoTracking().AsQueryable();
        if (request.InvoiceId.HasValue) query = query.Where(x => x.InvoiceId == request.InvoiceId);
        if (request.PaymentStatus.HasValue) query = query.Where(x => x.PaymentStatus == request.PaymentStatus);
        if (request.Date.HasValue) query = query.Where(x => x.PaidAtUtc.HasValue && x.PaidAtUtc.Value.Date == request.Date.Value.Date);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.PaymentId).Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new PaymentDto { PaymentId = x.PaymentId, InvoiceId = x.InvoiceId, PaymentMethodId = x.PaymentMethodId,
                Amount = x.Amount, PaymentStatus = x.PaymentStatus, TransactionCode = x.TransactionCode,
                PaidAtUtc = x.PaidAtUtc, ReceivedByUserId = x.ReceivedByUserId, Note = x.Note }).ToListAsync(ct);
        return Page(items, request.PageNumber, request.PageSize, total);
    }

    public Task<List<RevenueReportDto>> GetRevenueReportAsync(ReportQueryRequest request, CancellationToken ct)
    {
        var query = FilterInvoices(request).Where(x => x.PaymentStatus == InvoicePaymentStatuses.Paid && x.IssuedAtUtc.HasValue);
        return query.GroupBy(x => x.IssuedAtUtc!.Value.Date).OrderBy(x => x.Key)
            .Select(x => new RevenueReportDto { Date = x.Key, Revenue = x.Sum(i => i.PaidAmount), InvoiceCount = x.Count() }).ToListAsync(ct);
    }

    public Task<List<TableUsageReportDto>> GetTableUsageReportAsync(ReportQueryRequest request, CancellationToken ct)
    {
        var assignments = db.SessionTableAssignments.AsNoTracking().AsQueryable();
        if (request.FromDate.HasValue) assignments = assignments.Where(x => x.StartedAtUtc >= request.FromDate.Value);
        if (request.ToDate.HasValue) assignments = assignments.Where(x => x.StartedAtUtc < request.ToDate.Value.Date.AddDays(1));
        return (from item in assignments join table in db.VenueTables.AsNoTracking() on item.TableId equals table.TableId
                group item by new { table.TableId, table.TableName } into g orderby g.Sum(x => x.DurationMinutes ?? 0) descending
                select new TableUsageReportDto { TableId = g.Key.TableId, TableName = g.Key.TableName,
                    SessionCount = g.Select(x => x.SessionId).Distinct().Count(), TotalMinutes = g.Sum(x => x.DurationMinutes ?? 0) }).ToListAsync(ct);
    }

    public Task<List<ProductSalesReportDto>> GetProductReportAsync(ReportQueryRequest request, CancellationToken ct)
    {
        var orders = db.Orders.AsNoTracking().Where(x => x.Status != 3);
        if (request.FromDate.HasValue) orders = orders.Where(x => x.CreatedAtUtc >= request.FromDate.Value);
        if (request.ToDate.HasValue) orders = orders.Where(x => x.CreatedAtUtc < request.ToDate.Value.Date.AddDays(1));
        return (from item in db.OrderItems.AsNoTracking() join order in orders on item.OrderId equals order.OrderId
                group item by new { item.ProductId, item.ProductNameSnapshot } into g orderby g.Sum(x => x.Quantity) descending
                select new ProductSalesReportDto { ProductId = g.Key.ProductId, ProductName = g.Key.ProductNameSnapshot,
                    Quantity = g.Sum(x => x.Quantity), Revenue = g.Sum(x => x.LineTotalAmount) }).ToListAsync(ct);
    }

    public Task<List<BookingReportDto>> GetBookingReportAsync(ReportQueryRequest request, CancellationToken ct)
    {
        var query = db.Bookings.AsNoTracking().AsQueryable();
        if (request.FromDate.HasValue) query = query.Where(x => x.StartTimeUtc >= request.FromDate.Value);
        if (request.ToDate.HasValue) query = query.Where(x => x.StartTimeUtc < request.ToDate.Value.Date.AddDays(1));
        return query.GroupBy(x => x.Status).OrderBy(x => x.Key).Select(x => new BookingReportDto { Status = x.Key, Count = x.Count() }).ToListAsync(ct);
    }

    public Task<List<CustomerReportDto>> GetCustomerReportAsync(ReportQueryRequest request, CancellationToken ct)
    {
        var invoices = FilterInvoices(request);
        return db.Customers.AsNoTracking().Where(x => x.Status)
            .Select(customer => new CustomerReportDto
            {
                CustomerId = customer.CustomerId,
                CustomerName = customer.FullName,
                BookingCount = db.Bookings.Count(x => x.CustomerId == customer.CustomerId &&
                    (!request.FromDate.HasValue || x.StartTimeUtc >= request.FromDate.Value) &&
                    (!request.ToDate.HasValue || x.StartTimeUtc < request.ToDate.Value.Date.AddDays(1))),
                SessionCount = db.Sessions.Count(x => x.CustomerId == customer.CustomerId &&
                    (!request.FromDate.HasValue || x.StartedAtUtc >= request.FromDate.Value) &&
                    (!request.ToDate.HasValue || x.StartedAtUtc < request.ToDate.Value.Date.AddDays(1))),
                Revenue = invoices.Where(x => x.CustomerId == customer.CustomerId).Sum(x => (decimal?)x.PaidAmount) ?? 0
            })
            .OrderByDescending(x => x.Revenue)
            .ToListAsync(ct);
    }

    public Task<List<PaymentMethodReportDto>> GetPaymentMethodReportAsync(ReportQueryRequest request, CancellationToken ct)
    {
        var payments = db.Payments.AsNoTracking().Where(x => x.PaymentStatus == PaymentStatuses.Completed);
        if (request.FromDate.HasValue) payments = payments.Where(x => x.PaidAtUtc >= request.FromDate.Value);
        if (request.ToDate.HasValue) payments = payments.Where(x => x.PaidAtUtc < request.ToDate.Value.Date.AddDays(1));
        return (from payment in payments
                join method in db.PaymentMethods.AsNoTracking() on payment.PaymentMethodId equals method.PaymentMethodId
                group payment by new { method.PaymentMethodId, method.Name } into grouped
                orderby grouped.Sum(x => x.Amount) descending
                select new PaymentMethodReportDto
                {
                    PaymentMethodId = grouped.Key.PaymentMethodId,
                    PaymentMethodName = grouped.Key.Name,
                    PaymentCount = grouped.Count(),
                    Amount = grouped.Sum(x => x.Amount)
                }).ToListAsync(ct);
    }

    public Task<List<InventoryReportDto>> GetInventoryReportAsync(ReportQueryRequest request, CancellationToken ct)
    {
        var movements = db.InventoryTransactions.AsNoTracking().AsQueryable();
        if (request.FromDate.HasValue) movements = movements.Where(x => x.CreatedAtUtc >= request.FromDate.Value);
        if (request.ToDate.HasValue) movements = movements.Where(x => x.CreatedAtUtc < request.ToDate.Value.Date.AddDays(1));
        return db.Products.AsNoTracking().Where(x => x.IsActive && x.IsStockTracked)
            .Select(product => new InventoryReportDto
            {
                ProductId = product.ProductId,
                ProductName = product.Name,
                CurrentStock = product.StockQuantity,
                NetMovement = movements.Where(x => x.ProductId == product.ProductId).Sum(x => (int?)x.Quantity) ?? 0,
                InventoryValue = product.StockQuantity * product.UnitPrice
            })
            .OrderBy(x => x.ProductName)
            .ToListAsync(ct);
    }

    private IQueryable<PoolHub.Core.Entities.Invoice> FilterInvoices(ReportQueryRequest request)
    {
        var query = db.Invoices.AsNoTracking().AsQueryable();
        if (request.FromDate.HasValue) query = query.Where(x => x.IssuedAtUtc >= request.FromDate.Value);
        if (request.ToDate.HasValue) query = query.Where(x => x.IssuedAtUtc < request.ToDate.Value.Date.AddDays(1));
        return query;
    }

    private static void ValidateDiscountRequest(UpsertDiscountRequest request)
    {
        if (request.EndsAtUtc.HasValue && request.EndsAtUtc <= request.StartsAtUtc) throw new ValidationException("End date must be after start date.");
        var type = NormalizeDiscountType(request.DiscountType);
        if (type is not DiscountTypes.Percentage and not DiscountTypes.FixedAmount)
            throw new ValidationException("Discount type must be PERCENTAGE or FIXED_AMOUNT.");
        if (type == DiscountTypes.Percentage && request.Value > 100) throw new ValidationException("Percentage discount cannot exceed 100.");
    }

    private static void Apply(Discount entity, UpsertDiscountRequest request)
    {
        entity.Name = request.Name.Trim(); entity.DiscountType = NormalizeDiscountType(request.DiscountType);
        entity.Value = request.Value; entity.MaxAmount = request.MaxAmount; entity.MinTimeSubtotal = request.MinTimeSubtotal;
        entity.AppliesTo = "TIME"; entity.StartsAtUtc = request.StartsAtUtc; entity.EndsAtUtc = request.EndsAtUtc; entity.IsActive = request.IsActive;
    }

    private static DiscountDto MapDiscount(Discount x) => new() {
        DiscountId = x.DiscountId, DiscountCode = x.DiscountCode, Name = x.Name, DiscountType = x.DiscountType,
        Value = x.Value, MaxAmount = x.MaxAmount, MinTimeSubtotal = x.MinTimeSubtotal, AppliesTo = x.AppliesTo,
        StartsAtUtc = x.StartsAtUtc, EndsAtUtc = x.EndsAtUtc, IsActive = x.IsActive
    };
    private static PaymentMethodDto MapPaymentMethod(PaymentMethod x) => new() {
        PaymentMethodId = x.PaymentMethodId, Name = x.Name, Code = x.Code, Description = x.Description, IsActive = x.IsActive
    };
    private static void Normalize(PaginationRequest request) { request.PageNumber = Math.Max(1, request.PageNumber); request.PageSize = Math.Clamp(request.PageSize, 1, 100); }
    private static string NormalizeDiscountType(string value) =>
        value.Trim().ToUpperInvariant() == "FIXED" ? DiscountTypes.FixedAmount : value.Trim().ToUpperInvariant();
    private static PagedResult<T> Page<T>(List<T> items, int page, int size, int total) => new() { Items = items, PageNumber = page, PageSize = size, TotalItems = total };
}
