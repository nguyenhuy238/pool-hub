using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Order;
using PoolHub.Core.Entities;
using PoolHub.Infrastructure.Data;
using PoolHub.Services.Order;
using PoolHub.Shared.Exceptions;
using Xunit;

namespace PoolHub.UnitTests;

public class OrderServiceTests
{
    [Fact]
    public async Task AddOrderItemAsync_WhenQuantityIsNotPositive_ThrowsBusinessRuleException()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new PoolHubDbContext(options);
        db.Sessions.Add(new Session { SessionId = 1, Status = 1, StartedAtUtc = DateTime.UtcNow });
        db.Orders.Add(new Order { OrderId = 1, SessionId = 1, OrderedByUserId = 99, OrderCode = "OD1", Status = 1 });
        await db.SaveChangesAsync();

        var service = new OrderService(db);
        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.AddOrderItemAsync(1, new AddOrderItemRequest { ProductId = 1, Quantity = 0 }, CancellationToken.None));

        Assert.Equal("Quantity must be greater than zero.", exception.Message);
    }

    [Fact]
    public async Task AddOrderItemAsync_WhenSessionIsClosed_ThrowsBusinessRuleException()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new PoolHubDbContext(options);
        db.Sessions.Add(new Session { SessionId = 1, Status = 2, StartedAtUtc = DateTime.UtcNow.AddHours(-1), EndedAtUtc = DateTime.UtcNow });
        db.Orders.Add(new Order { OrderId = 1, SessionId = 1, OrderedByUserId = 99, OrderCode = "OD1", Status = 1 });
        db.Products.Add(new Product { ProductId = 1, ProductCategoryId = 1, Name = "Water", Sku = "WATER", UnitPrice = 10000, StockQuantity = 10, IsStockTracked = true, IsActive = true });
        await db.SaveChangesAsync();

        var service = new OrderService(db);
        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.AddOrderItemAsync(1, new AddOrderItemRequest { ProductId = 1, Quantity = 1 }, CancellationToken.None));

        Assert.Equal("Cannot modify orders for a closed session.", exception.Message);
    }
}
