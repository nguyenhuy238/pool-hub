using Microsoft.EntityFrameworkCore;
using PoolHub.Core.Entities;
using PoolHub.Shared.Constants;

namespace PoolHub.Infrastructure.Data.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(PoolHubDbContext db, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);
        if (await db.Roles.AnyAsync(ct)) return;

        string Hash(string p) => BCrypt.Net.BCrypt.HashPassword(p, 12);

        var roles = new[]
        {
            new Role { Name = RoleConstants.Admin, Description = "Full system admin", IsSystem = true },
            new Role { Name = RoleConstants.Manager, Description = "Operations manager", IsSystem = true },
            new Role { Name = RoleConstants.Staff, Description = "Floor staff", IsSystem = true },
            new Role { Name = RoleConstants.Cashier, Description = "Cashier", IsSystem = true }
        };
        db.Roles.AddRange(roles);
        await db.SaveChangesAsync(ct);

        var users = new[]
        {
            new User { FullName = "Admin", Email = "admin@poolhub.com", PasswordHash = Hash("Admin@123"), EmailConfirmed = true, Status = true },
            new User { FullName = "Manager", Email = "manager@poolhub.com", PasswordHash = Hash("Manager@123"), EmailConfirmed = true, Status = true },
            new User { FullName = "Staff 1", Email = "staff1@poolhub.com", PasswordHash = Hash("Staff@123"), EmailConfirmed = true, Status = true },
            new User { FullName = "Staff 2", Email = "staff2@poolhub.com", PasswordHash = Hash("Staff@123"), EmailConfirmed = true, Status = true },
            new User { FullName = "Cashier", Email = "cashier@poolhub.com", PasswordHash = Hash("Cashier@123"), EmailConfirmed = true, Status = true }
        };
        db.Users.AddRange(users);
        await db.SaveChangesAsync(ct);

        var roleMap = await db.Roles.ToDictionaryAsync(x => x.Name, x => x.RoleId, ct);
        var userMap = await db.Users.ToDictionaryAsync(x => x.Email, x => x.UserId, ct);

        db.UserRoles.AddRange([
            new UserRole { UserId = userMap["admin@poolhub.com"], RoleId = roleMap[RoleConstants.Admin] },
            new UserRole { UserId = userMap["manager@poolhub.com"], RoleId = roleMap[RoleConstants.Manager] },
            new UserRole { UserId = userMap["staff1@poolhub.com"], RoleId = roleMap[RoleConstants.Staff] },
            new UserRole { UserId = userMap["staff2@poolhub.com"], RoleId = roleMap[RoleConstants.Staff] },
            new UserRole { UserId = userMap["cashier@poolhub.com"], RoleId = roleMap[RoleConstants.Cashier] }
        ]);

        var floor = new Floor { Name = "Floor 1", DisplayOrder = 1, IsActive = true };
        var floor2 = new Floor { Name = "Floor 2 - VIP", DisplayOrder = 2, IsActive = true };
        db.Floors.AddRange(floor, floor2);
        await db.SaveChangesAsync(ct);

        var zoneA = new Zone { FloorId = floor.FloorId, Name = "Zone A", DisplayOrder = 1, IsActive = true };
        var zoneB = new Zone { FloorId = floor.FloorId, Name = "Zone B", DisplayOrder = 2, IsActive = true };
        var zoneC = new Zone { FloorId = floor.FloorId, Name = "Zone C - Smoking", DisplayOrder = 3, IsActive = true };
        var zoneD = new Zone { FloorId = floor2.FloorId, Name = "Zone D - Snooker", DisplayOrder = 1, IsActive = true };
        db.Zones.AddRange(zoneA, zoneB, zoneC, zoneD);

        var tt1 = new TableType { Name = "Pool Standard", Code = "POOL_STD", DefaultCapacity = 4 };
        var tt2 = new TableType { Name = "Pool VIP", Code = "POOL_VIP", DefaultCapacity = 6 };
        var tt3 = new TableType { Name = "Carom", Code = "CAROM", DefaultCapacity = 4 };
        var tt4 = new TableType { Name = "Snooker", Code = "SNOOKER", DefaultCapacity = 4 };
        db.TableTypes.AddRange(tt1, tt2, tt3, tt4);

        var plan = new PricingPlan { Name = "Default 2026", IsDefault = true, IsActive = true, StartsAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
        var weekendPlan = new PricingPlan { Name = "Weekend Plan", IsDefault = false, IsActive = true, StartsAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
        db.PricingPlans.AddRange(plan, weekendPlan);

        db.ProductCategories.AddRange(
            new ProductCategory { Name = "Drinks", Description = "Soft drinks", DisplayOrder = 1 },
            new ProductCategory { Name = "Snacks", Description = "Snacks", DisplayOrder = 2 },
            new ProductCategory { Name = "Services", Description = "Other services", DisplayOrder = 3 }
        );

        db.PaymentMethods.AddRange(
            new PaymentMethod { Name = "Cash", Code = "CASH" },
            new PaymentMethod { Name = "BankTransfer", Code = "BANK" },
            new PaymentMethod { Name = "EWallet", Code = "EWALLET" }
        );

        await db.SaveChangesAsync(ct);

        var tableTypes = await db.TableTypes.OrderBy(x => x.TableTypeId).ToListAsync(ct);
        var zones = await db.Zones.OrderBy(x => x.ZoneId).ToListAsync(ct);
        var categories = await db.ProductCategories.OrderBy(x => x.ProductCategoryId).ToListAsync(ct);

        db.PricingPlanRules.AddRange(
            new PricingPlanRule { PricingPlanId = plan.PricingPlanId, TableTypeId = tableTypes[0].TableTypeId, DayOfWeek = 1, StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(17), HourlyRate = 50000, MinimumMinutes = 30, BillingBlockMinutes = 15 },
            new PricingPlanRule { PricingPlanId = plan.PricingPlanId, TableTypeId = tableTypes[1].TableTypeId, DayOfWeek = 1, StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(17), HourlyRate = 90000, MinimumMinutes = 30, BillingBlockMinutes = 15 },
            new PricingPlanRule { PricingPlanId = plan.PricingPlanId, TableTypeId = tableTypes[2].TableTypeId, DayOfWeek = 1, StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(17), HourlyRate = 60000, MinimumMinutes = 30, BillingBlockMinutes = 15 },
            new PricingPlanRule { PricingPlanId = weekendPlan.PricingPlanId, TableTypeId = tableTypes[0].TableTypeId, DayOfWeek = 0, StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(17), HourlyRate = 70000, MinimumMinutes = 30, BillingBlockMinutes = 15 },
            new PricingPlanRule { PricingPlanId = weekendPlan.PricingPlanId, TableTypeId = tableTypes[3].TableTypeId, DayOfWeek = 0, StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(17), HourlyRate = 100000, MinimumMinutes = 30, BillingBlockMinutes = 15 }
        );

        for (var i = 1; i <= 18; i++)
        {
            long zoneId = zones[0].ZoneId;
            if (i > 5 && i <= 10) zoneId = zones[1].ZoneId; // Zone B
            if (i > 10 && i <= 15) zoneId = zones[2].ZoneId; // Zone C
            if (i > 15) zoneId = zones[3].ZoneId; // Zone D (Snooker)

            long tableTypeId = tableTypes[(i - 1) % 4].TableTypeId;
            if (i > 15) tableTypeId = tableTypes[3].TableTypeId; // Snooker tables in Zone D

            db.VenueTables.Add(new VenueTable
            {
                ZoneId = zoneId,
                TableTypeId = tableTypeId,
                TableCode = $"T{i:00}",
                TableName = $"Table {i:00}",
                Capacity = i > 10 ? 6 : 4,
                OperationalStatus = 1
            });

            if (i <= 10)
            {
                var categoryId = i <= 4 ? categories[0].ProductCategoryId : (i <= 7 ? categories[1].ProductCategoryId : categories[2].ProductCategoryId);
                db.Products.Add(new Product { ProductCategoryId = categoryId, Name = $"Product {i}", Sku = $"P{i:00}", UnitPrice = 10000 + (i * 2000), StockQuantity = 30 });
            }
        }
        await db.SaveChangesAsync(ct);

        var customers = new[]
        {
            new Customer { FullName = "Nguyen Van A", PhoneNumber = "0987654321", Status = true },
            new Customer { FullName = "Tran Thi B", PhoneNumber = "0912345678", Status = true },
            new Customer { FullName = "Le Van C", PhoneNumber = "0933334444", Status = true }
        };
        db.Customers.AddRange(customers);
        await db.SaveChangesAsync(ct);

        var venueTables = await db.VenueTables.OrderBy(x => x.TableId).ToListAsync(ct);
        var now = DateTime.UtcNow;

        db.Bookings.AddRange(
            new PoolHub.Core.Entities.Booking { CustomerId = customers[0].CustomerId, TableId = venueTables[0].TableId, TableTypeId = venueTables[0].TableTypeId, BookingCode = "BK1001", StartTimeUtc = now.AddHours(1), EndTimeUtc = now.AddHours(3), NumberOfGuests = 2, Status = 1 },
            new PoolHub.Core.Entities.Booking { CustomerId = customers[1].CustomerId, TableId = venueTables[1].TableId, TableTypeId = venueTables[1].TableTypeId, BookingCode = "BK1002", StartTimeUtc = now.AddHours(2), EndTimeUtc = now.AddHours(4), NumberOfGuests = 4, Status = 2 },
            new PoolHub.Core.Entities.Booking { CustomerId = customers[2].CustomerId, TableId = venueTables[2].TableId, TableTypeId = venueTables[2].TableTypeId, BookingCode = "BK1003", StartTimeUtc = now.AddDays(1).AddHours(1), EndTimeUtc = now.AddDays(1).AddHours(2), NumberOfGuests = 3, Status = 3 },
            new PoolHub.Core.Entities.Booking { CustomerId = customers[0].CustomerId, TableId = venueTables[3].TableId, TableTypeId = venueTables[3].TableTypeId, BookingCode = "BK1004", StartTimeUtc = now.AddDays(1).AddHours(3), EndTimeUtc = now.AddDays(1).AddHours(5), NumberOfGuests = 2, Status = 2 },
            new PoolHub.Core.Entities.Booking { CustomerId = customers[1].CustomerId, TableId = venueTables[15].TableId, TableTypeId = venueTables[15].TableTypeId, BookingCode = "BK1005", StartTimeUtc = now.AddHours(5), EndTimeUtc = now.AddHours(7), NumberOfGuests = 5, Status = 1 }
        );
        await db.SaveChangesAsync(ct);

        // Seed Discounts
        var discounts = new[]
        {
            new Discount { DiscountCode = "DISCOUNT10", Name = "10% off Table Time", DiscountType = "PERCENTAGE", Value = 10, AppliesTo = "TIME", StartsAtUtc = now.AddDays(-10), EndsAtUtc = now.AddDays(10), IsActive = true },
            new Discount { DiscountCode = "POOLVIP20", Name = "20% off Total Bill", DiscountType = "PERCENTAGE", Value = 20, AppliesTo = "ALL", StartsAtUtc = now.AddDays(-5), IsActive = true, MaxAmount = 50000 },
            new Discount { DiscountCode = "FIXED50K", Name = "50K Fixed Discount", DiscountType = "FIXED", Value = 50000, AppliesTo = "ALL", StartsAtUtc = now.AddDays(-10), IsActive = true, MinTimeSubtotal = 100000 }
        };
        db.Discounts.AddRange(discounts);
        await db.SaveChangesAsync(ct);

        // Seed Completed Session (Table 1 - Index 0)
        var closedSession = new Session
        {
            SessionCode = "SS202606080001",
            CustomerId = customers[0].CustomerId,
            Status = 2, // Closed
            StartedAtUtc = now.AddHours(-3),
            EndedAtUtc = now.AddHours(-1),
            OpenedByUserId = userMap.Values.First(),
            ClosedByUserId = userMap.Values.First()
        };
        db.Sessions.Add(closedSession);
        await db.SaveChangesAsync(ct);

        var assignment1 = new SessionTableAssignment
        {
            SessionId = closedSession.SessionId,
            TableId = venueTables[0].TableId,
            StartedAtUtc = closedSession.StartedAtUtc,
            EndedAtUtc = closedSession.EndedAtUtc,
            DurationMinutes = 120,
            HourlyRateSnapshot = 50000,
            Amount = 100000,
            AssignedByUserId = userMap.Values.First()
        };
        db.SessionTableAssignments.Add(assignment1);

        var product = await db.Products.FirstAsync(ct);
        var order1 = new Order
        {
            SessionId = closedSession.SessionId,
            OrderCode = "OD202606080001",
            OrderedByUserId = userMap.Values.First(),
            Status = 2, // Completed
            SubtotalAmount = product.UnitPrice * 2
        };
        db.Orders.Add(order1);
        await db.SaveChangesAsync(ct);

        var orderItem1 = new OrderItem
        {
            OrderId = order1.OrderId,
            ProductId = product.ProductId,
            ProductNameSnapshot = product.Name,
            UnitPriceSnapshot = product.UnitPrice,
            Quantity = 2,
            LineTotalAmount = product.UnitPrice * 2
        };
        db.OrderItems.Add(orderItem1);

        var invoice1 = new Invoice
        {
            SessionId = closedSession.SessionId,
            CustomerId = customers[0].CustomerId,
            InvoiceCode = "INV202606080001",
            TimeSubtotalAmount = 100000,
            ProductSubtotalAmount = product.UnitPrice * 2,
            SubtotalAmount = 100000 + (product.UnitPrice * 2),
            DiscountAmount = 0,
            TaxAmount = 0,
            GrandTotalAmount = 100000 + (product.UnitPrice * 2),
            PaidAmount = 100000 + (product.UnitPrice * 2),
            PaymentStatus = 2, // Paid
            Status = 2, // Completed
            IssuedByUserId = userMap.Values.First(),
            IssuedAtUtc = now.AddHours(-1)
        };
        db.Invoices.Add(invoice1);
        await db.SaveChangesAsync(ct);

        db.InvoiceLines.AddRange(
            new InvoiceLine { InvoiceId = invoice1.InvoiceId, LineType = "TIME", ReferenceId = assignment1.SessionTableAssignmentId, Description = $"Time played on table {venueTables[0].TableName}", Quantity = 2m, UnitPrice = 50000, LineTotalAmount = 100000 },
            new InvoiceLine { InvoiceId = invoice1.InvoiceId, LineType = "PRODUCT", ReferenceId = orderItem1.OrderItemId, Description = product.Name, Quantity = 2m, UnitPrice = product.UnitPrice, LineTotalAmount = product.UnitPrice * 2 }
        );

        var paymentMethods = await db.PaymentMethods.ToListAsync(ct);
        db.Payments.Add(new Payment
        {
            InvoiceId = invoice1.InvoiceId,
            PaymentMethodId = paymentMethods[0].PaymentMethodId, // Cash
            Amount = invoice1.GrandTotalAmount,
            PaymentStatus = 2, // Completed
            PaidAtUtc = now.AddHours(-1),
            ReceivedByUserId = userMap.Values.First()
        });

        // Seed Active Session (Table 2 - Index 1)
        var activeSession = new Session
        {
            SessionCode = "SS202606080002",
            CustomerId = customers[1].CustomerId,
            Status = 1, // Active
            StartedAtUtc = now.AddMinutes(-30),
            OpenedByUserId = userMap.Values.First()
        };
        db.Sessions.Add(activeSession);
        
        // Mark Table 2 as Occupied
        venueTables[1].OperationalStatus = 2; // Occupied

        await db.SaveChangesAsync(ct);

        db.SessionTableAssignments.Add(new SessionTableAssignment
        {
            SessionId = activeSession.SessionId,
            TableId = venueTables[1].TableId,
            StartedAtUtc = activeSession.StartedAtUtc,
            HourlyRateSnapshot = 90000,
            AssignedByUserId = userMap.Values.First()
        });

        await db.SaveChangesAsync(ct);
    }
}
