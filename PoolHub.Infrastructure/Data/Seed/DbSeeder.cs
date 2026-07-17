using Microsoft.EntityFrameworkCore;
using PoolHub.Core.Entities;
using PoolHub.Core.Enums;
using PoolHub.Shared.Constants;

namespace PoolHub.Infrastructure.Data.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(PoolHubDbContext db, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);
        await EnsureRolesAsync(db, ct);
        await EnsurePermissionsAsync(db, ct);
        await EnsureDemoUsersAsync(db, ct);
        await EnsureDemoDiscountsAsync(db, ct);
        await EnsureBankTransferPaymentMethodAsync(db, ct);
        await EnsureDepositPaymentMethodAsync(db, ct);

        var userMap = await db.Users.ToDictionaryAsync(x => x.Email, x => x.UserId, ct);

        if (await db.Floors.AnyAsync(ct))
        {
            await EnsureDemoVenueLayoutAsync(db, ct);
            await EnsureDefaultPricingCoverageAsync(db, ct);
            await EnsureDemoCustomerReviewsAsync(db, ct);
            return;
        }

        var floor = new Floor { Name = "Floor 1", DisplayOrder = 1, IsActive = true };
        db.Floors.Add(floor);
        await db.SaveChangesAsync(ct);

        var zoneA = new Zone { FloorId = floor.FloorId, Name = "Zone A", DisplayOrder = 1, IsActive = true };
        var zoneB = new Zone { FloorId = floor.FloorId, Name = "Zone B", DisplayOrder = 2, IsActive = true };
        db.Zones.AddRange(zoneA, zoneB);

        var tt1 = new TableType { Name = "Pool Standard", Code = "POOL_STD", DefaultCapacity = 4 };
        var tt2 = new TableType { Name = "Pool VIP", Code = "POOL_VIP", DefaultCapacity = 6 };
        var tt3 = new TableType { Name = "Carom", Code = "CAROM", DefaultCapacity = 4 };
        db.TableTypes.AddRange(tt1, tt2, tt3);

        var plan = new PricingPlan { Name = "Default 2026", IsDefault = true, IsActive = true, StartsAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
        db.PricingPlans.Add(plan);

        db.ProductCategories.AddRange(
            new ProductCategory { Name = "Drinks", Description = "Soft drinks", DisplayOrder = 1 },
            new ProductCategory { Name = "Snacks", Description = "Snacks", DisplayOrder = 2 },
            new ProductCategory { Name = "Services", Description = "Other services", DisplayOrder = 3 }
        );

        db.PaymentMethods.AddRange(
            new PaymentMethod { Name = "Cash", Code = "CASH" },
            new PaymentMethod { Name = "BankTransfer", Code = "BANK", Description = DefaultBankTransferDescription },
            new PaymentMethod { Name = "EWallet", Code = "EWALLET" }
        );

        await db.SaveChangesAsync(ct);

        var tableTypes = await db.TableTypes.OrderBy(x => x.TableTypeId).ToListAsync(ct);
        var zones = await db.Zones.OrderBy(x => x.ZoneId).ToListAsync(ct);
        var categories = await db.ProductCategories.OrderBy(x => x.ProductCategoryId).ToListAsync(ct);

        db.PricingPlanRules.AddRange(
            new PricingPlanRule { PricingPlanId = plan.PricingPlanId, TableTypeId = tableTypes[0].TableTypeId, DayOfWeek = 1, StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(17), HourlyRate = 50000, MinimumMinutes = 30, BillingBlockMinutes = 15 },
            new PricingPlanRule { PricingPlanId = plan.PricingPlanId, TableTypeId = tableTypes[1].TableTypeId, DayOfWeek = 1, StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(17), HourlyRate = 90000, MinimumMinutes = 30, BillingBlockMinutes = 15 },
            new PricingPlanRule { PricingPlanId = plan.PricingPlanId, TableTypeId = tableTypes[2].TableTypeId, DayOfWeek = 1, StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(17), HourlyRate = 60000, MinimumMinutes = 30, BillingBlockMinutes = 15 }
        );

        for (var i = 1; i <= 10; i++)
        {
            db.VenueTables.Add(new VenueTable
            {
                ZoneId = i <= 5 ? zones[0].ZoneId : zones[1].ZoneId,
                TableTypeId = tableTypes[(i - 1) % 3].TableTypeId,
                TableCode = $"T{i:00}",
                TableName = $"Table {i:00}",
                Capacity = 4,
                OperationalStatus = 1
            });

            var categoryId = i <= 4 ? categories[0].ProductCategoryId : (i <= 7 ? categories[1].ProductCategoryId : categories[2].ProductCategoryId);
            db.Products.Add(new Product { ProductCategoryId = categoryId, Name = $"Product {i}", Sku = $"P{i:00}", UnitPrice = 10000 + (i * 2000), StockQuantity = 30 });
        }

        await db.SaveChangesAsync(ct);
    }

    private const string DefaultBankTransferDescription =
        "{\"vietqr\":true,\"bankCode\":\"MB\",\"bankName\":\"MB Bank\",\"accountNo\":\"989420048989\",\"accountName\":\"POOLHUB\"}";

    private static async Task EnsureBankTransferPaymentMethodAsync(PoolHubDbContext db, CancellationToken ct)
    {
        var method = await db.PaymentMethods.FirstOrDefaultAsync(x => x.Code == "BANK", ct);
        if (method is null)
        {
            db.PaymentMethods.Add(new PaymentMethod
            {
                Name = "BankTransfer",
                Code = "BANK",
                Description = DefaultBankTransferDescription,
                IsActive = true
            });
            await db.SaveChangesAsync(ct);
            return;
        }

        if (string.IsNullOrWhiteSpace(method.Description) || !method.Description.TrimStart().StartsWith('{'))
        {
            method.Description = DefaultBankTransferDescription;
            method.IsActive = true;
            await db.SaveChangesAsync(ct);
        }
    }
}
