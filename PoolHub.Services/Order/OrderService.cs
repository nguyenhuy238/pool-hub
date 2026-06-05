using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Order;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared.Exceptions;
using EntityOrder = PoolHub.Core.Entities.Order;

namespace PoolHub.Services.Order;

public class OrderService(PoolHubDbContext db) : IOrderService
{
    public async Task<OrderDto> CreateOrderAsync(long userId, CreateOrderRequest request, CancellationToken ct)
    {
        var order = new EntityOrder { SessionId = request.SessionId, OrderedByUserId = userId, OrderCode = $"OD{DateTime.UtcNow:yyyyMMddHHmmss}", Status = 1 };
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
