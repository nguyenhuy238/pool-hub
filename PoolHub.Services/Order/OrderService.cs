using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Order;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EntityOrder = PoolHub.Core.Entities.Order;

namespace PoolHub.Services.Order;

public class OrderService(PoolHubDbContext db, IPosNotificationService posNotificationService) : IOrderService
{
    public async Task<OrderDetailDto> GetOrderByIdAsync(long orderId, CancellationToken ct)
    {
        var order = await db.Orders.FindAsync([orderId], ct) ?? throw new NotFoundException("Order not found.");
        var items = await db.OrderItems
            .Where(x => x.OrderId == orderId)
            .Select(x => new OrderItemDto
            {
                OrderItemId = x.OrderItemId,
                OrderId = x.OrderId,
                ProductId = x.ProductId,
                ProductNameSnapshot = x.ProductNameSnapshot,
                UnitPriceSnapshot = x.UnitPriceSnapshot,
                Quantity = x.Quantity,
                LineTotalAmount = x.LineTotalAmount,
                Note = x.Note
            })
            .ToListAsync(ct);

        return new OrderDetailDto
        {
            OrderId = order.OrderId,
            OrderCode = order.OrderCode,
            SessionId = order.SessionId,
            OrderedByUserId = order.OrderedByUserId,
            Status = order.Status,
            SubtotalAmount = order.SubtotalAmount,
            Note = order.Note,
            Items = items
        };
    }

    public async Task<List<OrderDetailDto>> GetOrdersBySessionIdAsync(long sessionId, CancellationToken ct)
    {
        var orders = await db.Orders.Where(x => x.SessionId == sessionId).ToListAsync(ct);
        var details = new List<OrderDetailDto>();

        foreach (var order in orders)
        {
            var items = await db.OrderItems
                .Where(x => x.OrderId == order.OrderId)
                .Select(x => new OrderItemDto
                {
                    OrderItemId = x.OrderItemId,
                    OrderId = x.OrderId,
                    ProductId = x.ProductId,
                    ProductNameSnapshot = x.ProductNameSnapshot,
                    UnitPriceSnapshot = x.UnitPriceSnapshot,
                    Quantity = x.Quantity,
                    LineTotalAmount = x.LineTotalAmount,
                    Note = x.Note
                })
                .ToListAsync(ct);

            details.Add(new OrderDetailDto
            {
                OrderId = order.OrderId,
                OrderCode = order.OrderCode,
                SessionId = order.SessionId,
                OrderedByUserId = order.OrderedByUserId,
                Status = order.Status,
                SubtotalAmount = order.SubtotalAmount,
                Note = order.Note,
                Items = items
            });
        }

        return details;
    }

    public async Task<OrderDto> CreateOrderAsync(long userId, CreateOrderRequest request, CancellationToken ct)
    {
        var session = await db.Sessions.FindAsync([request.SessionId], ct) ?? throw new NotFoundException("Session not found.");
        if (session.Status != 1)
        {
            throw new BusinessRuleException("Cannot create order for an inactive session.");
        }

        var order = new EntityOrder 
        { 
            SessionId = request.SessionId, 
            OrderedByUserId = userId, 
            OrderCode = $"OD{DateTime.UtcNow:yyyyMMddHHmmss}", 
            Status = 1 
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
        await posNotificationService.NotifySessionUpdateAsync((int)order.SessionId, ct);

        return new OrderDto { OrderId = order.OrderId, SessionId = order.SessionId, Status = order.Status };
    }

    public async Task AddOrderItemAsync(long orderId, AddOrderItemRequest request, CancellationToken ct)
    {
        if (request.Quantity <= 0)
        {
            throw new BusinessRuleException("Quantity must be greater than zero.");
        }

        var order = await db.Orders.FindAsync([orderId], ct) ?? throw new NotFoundException("Order not found.");
        await EnsureOrderEditableAsync(order, "add items to", ct);

        var product = await db.Products.FindAsync([request.ProductId], ct) ?? throw new NotFoundException("Product not found.");
        if (product.IsStockTracked && product.StockQuantity < request.Quantity) 
        {
            throw new BusinessRuleException("Not enough stock quantity.");
        }

        var lineTotal = product.UnitPrice * request.Quantity;
        var item = new OrderItem 
        { 
            OrderId = orderId, 
            ProductId = request.ProductId, 
            ProductNameSnapshot = product.Name, 
            Quantity = request.Quantity, 
            UnitPriceSnapshot = product.UnitPrice, 
            LineTotalAmount = lineTotal 
        };
        
        order.SubtotalAmount += lineTotal;
        if (product.IsStockTracked) 
        {
            product.StockQuantity -= request.Quantity;
        }

        db.OrderItems.Add(item);
        db.InventoryTransactions.Add(new InventoryTransaction 
        { 
            ProductId = request.ProductId, 
            TransactionType = 4, 
            Quantity = -request.Quantity, 
            ReferenceType = "ORDER", 
            ReferenceId = orderId, 
            Note = "ORDER ITEM ADDED", 
            CreatedByUserId = order.OrderedByUserId 
        });

        await db.SaveChangesAsync(ct);
        await posNotificationService.NotifySessionUpdateAsync((int)order.SessionId, ct);

    }

    public async Task UpdateOrderItemAsync(long orderId, long itemId, int quantity, CancellationToken ct)
    {
        if (quantity <= 0)
        {
            throw new BusinessRuleException("Quantity must be greater than zero. Use DELETE endpoint to remove items.");
        }

        var order = await db.Orders.FindAsync([orderId], ct) ?? throw new NotFoundException("Order not found.");
        await EnsureOrderEditableAsync(order, "update items on", ct);

        var item = await db.OrderItems.FirstOrDefaultAsync(x => x.OrderItemId == itemId && x.OrderId == orderId, ct) ?? throw new NotFoundException("Order item not found.");
        var product = await db.Products.FindAsync([item.ProductId], ct) ?? throw new NotFoundException("Product not found.");

        int diff = quantity - item.Quantity;

        if (diff > 0)
        {
            if (product.IsStockTracked && product.StockQuantity < diff)
            {
                throw new BusinessRuleException("Not enough stock quantity.");
            }
            if (product.IsStockTracked)
            {
                product.StockQuantity -= diff;
            }
        }
        else if (diff < 0)
        {
            if (product.IsStockTracked)
            {
                product.StockQuantity += -diff;
            }
        }

        item.Quantity = quantity;
        item.LineTotalAmount = item.UnitPriceSnapshot * quantity;

        // Recalculate order subtotal
        order.SubtotalAmount += diff * item.UnitPriceSnapshot;

        // Log transaction
        db.InventoryTransactions.Add(new InventoryTransaction
        {
            ProductId = item.ProductId,
            TransactionType = 4,
            Quantity = -diff,
            ReferenceType = "ORDER",
            ReferenceId = orderId,
            Note = "ORDER ITEM UPDATED",
            CreatedByUserId = order.OrderedByUserId
        });

        await db.SaveChangesAsync(ct);
        await posNotificationService.NotifySessionUpdateAsync((int)order.SessionId, ct);

    }

    public async Task DeleteOrderItemAsync(long orderId, long itemId, CancellationToken ct)
    {
        var order = await db.Orders.FindAsync([orderId], ct) ?? throw new NotFoundException("Order not found.");
        await EnsureOrderEditableAsync(order, "delete items from", ct);

        var item = await db.OrderItems.FirstOrDefaultAsync(x => x.OrderItemId == itemId && x.OrderId == orderId, ct) ?? throw new NotFoundException("Order item not found.");
        var product = await db.Products.FindAsync([item.ProductId], ct) ?? throw new NotFoundException("Product not found.");

        if (product.IsStockTracked)
        {
            product.StockQuantity += item.Quantity;
        }

        order.SubtotalAmount -= item.LineTotalAmount;

        // Log transaction for stock return
        db.InventoryTransactions.Add(new InventoryTransaction
        {
            ProductId = item.ProductId,
            TransactionType = 4,
            Quantity = item.Quantity,
            ReferenceType = "ORDER",
            ReferenceId = orderId,
            Note = "ORDER ITEM REMOVED",
            CreatedByUserId = order.OrderedByUserId
        });

        db.OrderItems.Remove(item);
        await db.SaveChangesAsync(ct);
        await posNotificationService.NotifySessionUpdateAsync((int)order.SessionId, ct);

    }

    public async Task CancelOrderAsync(long orderId, CancellationToken ct)
    {
        var order = await db.Orders.FindAsync([orderId], ct) ?? throw new NotFoundException("Order not found.");
        if (order.Status == 3)
        {
            return; // Already cancelled
        }
        if (order.Status == 2)
        {
            throw new BusinessRuleException("Cannot cancel a completed/paid order.");
        }
        await EnsureSessionActiveAsync(order.SessionId, ct);

        order.Status = 3; // Cancelled
        var items = await db.OrderItems.Where(x => x.OrderId == orderId).ToListAsync(ct);

        foreach (var item in items)
        {
            var product = await db.Products.FindAsync([item.ProductId], ct);
            if (product != null)
            {
                if (product.IsStockTracked)
                {
                    product.StockQuantity += item.Quantity;
                }

                // Log transaction
                db.InventoryTransactions.Add(new InventoryTransaction
                {
                    ProductId = item.ProductId,
                    TransactionType = 4,
                    Quantity = item.Quantity,
                    ReferenceType = "ORDER",
                    ReferenceId = orderId,
                    Note = "ORDER CANCELLED",
                    CreatedByUserId = order.OrderedByUserId
                });
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureOrderEditableAsync(EntityOrder order, string action, CancellationToken ct)
    {
        if (order.Status != 1)
        {
            throw new BusinessRuleException($"Cannot {action} a completed or cancelled order.");
        }

        await EnsureSessionActiveAsync(order.SessionId, ct);
    }

    private async Task EnsureSessionActiveAsync(long sessionId, CancellationToken ct)
    {
        var session = await db.Sessions.FindAsync([sessionId], ct) ?? throw new NotFoundException("Session not found.");
        if (session.Status != 1)
        {
            throw new BusinessRuleException("Cannot modify orders for a closed session.");
        }
    }
}
