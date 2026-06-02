using PoolHub.Core.DTOs.Order;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Repositories;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared.Exceptions;

namespace PoolHub.Services.Services.Order;

public class OrderService(IOrderRepository orderRepo, IProductRepository productRepo) : IOrderService
{
    public async Task<OrderDto> CreateOrderAsync(int userId, CreateOrderRequest request, CancellationToken ct)
    {
        var order = new PoolHub.Core.Entities.Order { SessionId = request.SessionId, CreatedByUserId = userId, Status = 1 };
        await orderRepo.AddOrderAsync(order, ct);
        await orderRepo.SaveChangesAsync(ct);
        return new OrderDto { OrderId = order.OrderId, SessionId = order.SessionId, Status = order.Status };
    }

    public async Task AddOrderItemAsync(int orderId, AddOrderItemRequest request, CancellationToken ct)
    {
        var product = await productRepo.GetProductByIdAsync(request.ProductId, ct) ?? throw new NotFoundException("Product not found.");
        if (product.StockQuantity < request.Quantity) throw new BusinessRuleException("Not enough stock quantity.");
        
        var item = new OrderItem { OrderId = orderId, ProductId = request.ProductId, Quantity = request.Quantity, UnitPrice = product.UnitPrice, LineTotal = product.UnitPrice * request.Quantity };
        product.StockQuantity -= request.Quantity;
        
        await orderRepo.AddOrderItemAsync(item, ct);
        await productRepo.AddInventoryTransactionAsync(new InventoryTransaction { ProductId = request.ProductId, QuantityChange = -request.Quantity, StockAfter = product.StockQuantity, Reason = "ORDER" }, ct);
        
        await orderRepo.SaveChangesAsync(ct);
        // Note: productRepo and orderRepo likely share the same DbContext in DI, so SaveChangesAsync on either works.
    }
}
