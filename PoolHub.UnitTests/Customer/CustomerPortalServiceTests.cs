using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Audit;
using PoolHub.Core.DTOs.Customer;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Services.Customer;
using PoolHub.Shared;

namespace PoolHub.UnitTests;

public class CustomerPortalServiceTests
{
    [Fact]
    public async Task UpdatePortalProfileAsync_UpdatesCustomerAndLinkedUser()
    {
        await using var db = CreateDb();
        await SeedCustomerAccountAsync(db);
        var service = new CustomerService(db, new NoOpAuditService());

        var result = await service.UpdatePortalProfileAsync(10, new UpdateCustomerPortalProfileRequest
        {
            FullName = "Nguyễn An",
            PhoneNumber = "0912345678"
        }, CancellationToken.None);

        Assert.Equal("Nguyễn An", result.FullName);
        Assert.Equal("0912345678", result.PhoneNumber);
        Assert.Equal("Nguyễn An", (await db.Users.SingleAsync()).FullName);
        Assert.Equal("0912345678", (await db.Customers.SingleAsync()).PhoneNumber);
    }

    [Fact]
    public async Task ExchangePortalVoucherAsync_DeductsPointsAndCreatesPersonalVoucher()
    {
        await using var db = CreateDb();
        await SeedCustomerAccountAsync(db);
        db.Discounts.Add(new Discount
        {
            DiscountId = 20,
            DiscountCode = "TEMPLATE-10",
            Name = "Giảm 10%",
            DiscountType = "PERCENTAGE",
            Value = 10,
            AppliesTo = "TIME",
            StartsAtUtc = DateTime.UtcNow.AddDays(-1),
            EndsAtUtc = DateTime.UtcNow.AddDays(10),
            IsActive = true,
            IsVoucher = true,
            PointsRequired = 100
        });
        await db.SaveChangesAsync();
        var service = new CustomerService(db, new NoOpAuditService());

        var voucher = await service.ExchangePortalVoucherAsync(10, 20, CancellationToken.None);

        Assert.Equal(400, (await db.Customers.SingleAsync()).LoyaltyPoints);
        Assert.Equal(1, await db.CustomerPointHistories.CountAsync());
        Assert.Equal(1, await db.Discounts.CountAsync(x => x.CustomerId == 1));
        Assert.Equal(1, voucher.CustomerId);
        Assert.StartsWith("V-1-", voucher.DiscountCode);
    }

    private static PoolHubDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PoolHubDbContext(options);
    }

    private static async Task SeedCustomerAccountAsync(PoolHubDbContext db)
    {
        db.Users.Add(new User
        {
            UserId = 10,
            FullName = "Khách hàng",
            Email = "customer@poolhub.test",
            PhoneNumber = "0900000000",
            PasswordHash = "test"
        });
        db.Customers.Add(new Customer
        {
            CustomerId = 1,
            UserId = 10,
            FullName = "Khách hàng",
            Email = "customer@poolhub.test",
            PhoneNumber = "0900000000",
            Status = true,
            LoyaltyPoints = 500,
            TotalPointsEarned = 500
        });
        await db.SaveChangesAsync();
    }

    private sealed class NoOpAuditService : IAuditService
    {
        public Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(
            AuditLogQueryRequest request, CancellationToken ct) =>
            Task.FromResult(new PagedResult<AuditLogDto>());

        public Task<AuditLogDto> GetAuditLogAsync(long id, CancellationToken ct) =>
            Task.FromResult(new AuditLogDto());

        public Task LogAsync(
            long? actorUserId,
            string action,
            string entityName,
            long? entityId = null,
            Guid? entityPublicId = null,
            object? oldValues = null,
            object? newValues = null,
            string? description = null,
            CancellationToken ct = default) =>
            Task.CompletedTask;
    }
}
