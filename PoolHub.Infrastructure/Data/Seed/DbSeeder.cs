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

        var floor = new Floor { Name = "Tầng 1", DisplayOrder = 1, IsActive = true };
        var floor2 = new Floor { Name = "Tầng 2 - VIP", DisplayOrder = 2, IsActive = true };
        db.Floors.AddRange(floor, floor2);
        await db.SaveChangesAsync(ct);

        var zoneA = new Zone { FloorId = floor.FloorId, Name = "Khu A", DisplayOrder = 1, IsActive = true };
        var zoneB = new Zone { FloorId = floor.FloorId, Name = "Khu B", DisplayOrder = 2, IsActive = true };
        var zoneC = new Zone { FloorId = floor.FloorId, Name = "Khu C - Hút thuốc", DisplayOrder = 3, IsActive = true };
        var zoneD = new Zone { FloorId = floor2.FloorId, Name = "Khu D - Snooker", DisplayOrder = 1, IsActive = true };
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
            new PaymentMethod { Name = "BankTransfer", Code = "BANK", Description = DefaultBankTransferDescription },
            new PaymentMethod { Name = "EWallet", Code = "EWALLET" }
        );

        await db.SaveChangesAsync(ct);

        var tableTypes = await db.TableTypes.OrderBy(x => x.TableTypeId).ToListAsync(ct);
        var zones = await db.Zones.OrderBy(x => x.ZoneId).ToListAsync(ct);
        var categories = await db.ProductCategories.OrderBy(x => x.ProductCategoryId).ToListAsync(ct);

        var rules = new List<PricingPlanRule>();
        
        // Weekdays: 1 (Mon) to 5 (Fri)
        for(int d = 1; d <= 5; d++)
        {
            // Ca Sáng (08:00 - 13:00)
            rules.Add(new PricingPlanRule { PricingPlanId = plan.PricingPlanId, TableTypeId = tableTypes[0].TableTypeId, DayOfWeek = d, StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(13), HourlyRate = 40000, MinimumMinutes = 30, BillingBlockMinutes = 15 });
            rules.Add(new PricingPlanRule { PricingPlanId = plan.PricingPlanId, TableTypeId = tableTypes[2].TableTypeId, DayOfWeek = d, StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(13), HourlyRate = 50000, MinimumMinutes = 30, BillingBlockMinutes = 15 });
            rules.Add(new PricingPlanRule { PricingPlanId = plan.PricingPlanId, TableTypeId = tableTypes[1].TableTypeId, DayOfWeek = d, StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(13), HourlyRate = 70000, MinimumMinutes = 30, BillingBlockMinutes = 15 });
            rules.Add(new PricingPlanRule { PricingPlanId = plan.PricingPlanId, TableTypeId = tableTypes[3].TableTypeId, DayOfWeek = d, StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(13), HourlyRate = 80000, MinimumMinutes = 30, BillingBlockMinutes = 15 });
            
            // Ca Chiều (13:00 - 18:00)
            rules.Add(new PricingPlanRule { PricingPlanId = plan.PricingPlanId, TableTypeId = tableTypes[0].TableTypeId, DayOfWeek = d, StartTime = TimeSpan.FromHours(13), EndTime = TimeSpan.FromHours(18), HourlyRate = 50000, MinimumMinutes = 30, BillingBlockMinutes = 15 });
            rules.Add(new PricingPlanRule { PricingPlanId = plan.PricingPlanId, TableTypeId = tableTypes[2].TableTypeId, DayOfWeek = d, StartTime = TimeSpan.FromHours(13), EndTime = TimeSpan.FromHours(18), HourlyRate = 60000, MinimumMinutes = 30, BillingBlockMinutes = 15 });
            rules.Add(new PricingPlanRule { PricingPlanId = plan.PricingPlanId, TableTypeId = tableTypes[1].TableTypeId, DayOfWeek = d, StartTime = TimeSpan.FromHours(13), EndTime = TimeSpan.FromHours(18), HourlyRate = 90000, MinimumMinutes = 30, BillingBlockMinutes = 15 });
            rules.Add(new PricingPlanRule { PricingPlanId = plan.PricingPlanId, TableTypeId = tableTypes[3].TableTypeId, DayOfWeek = d, StartTime = TimeSpan.FromHours(13), EndTime = TimeSpan.FromHours(18), HourlyRate = 90000, MinimumMinutes = 30, BillingBlockMinutes = 15 });
            
            // Ca Tối (18:00 - 24:00)
            rules.Add(new PricingPlanRule { PricingPlanId = plan.PricingPlanId, TableTypeId = tableTypes[0].TableTypeId, DayOfWeek = d, StartTime = TimeSpan.FromHours(18), EndTime = new TimeSpan(23, 59, 59), HourlyRate = 60000, MinimumMinutes = 30, BillingBlockMinutes = 15 });
            rules.Add(new PricingPlanRule { PricingPlanId = plan.PricingPlanId, TableTypeId = tableTypes[2].TableTypeId, DayOfWeek = d, StartTime = TimeSpan.FromHours(18), EndTime = new TimeSpan(23, 59, 59), HourlyRate = 70000, MinimumMinutes = 30, BillingBlockMinutes = 15 });
            rules.Add(new PricingPlanRule { PricingPlanId = plan.PricingPlanId, TableTypeId = tableTypes[1].TableTypeId, DayOfWeek = d, StartTime = TimeSpan.FromHours(18), EndTime = new TimeSpan(23, 59, 59), HourlyRate = 100000, MinimumMinutes = 30, BillingBlockMinutes = 15 });
            rules.Add(new PricingPlanRule { PricingPlanId = plan.PricingPlanId, TableTypeId = tableTypes[3].TableTypeId, DayOfWeek = d, StartTime = TimeSpan.FromHours(18), EndTime = new TimeSpan(23, 59, 59), HourlyRate = 110000, MinimumMinutes = 30, BillingBlockMinutes = 15 });
        }
        
        // Weekends: 6 (Sat), 0 (Sun)
        int[] weekendDays = { 0, 6 };
        foreach(var d in weekendDays)
        {
            // Ca Sáng
            rules.Add(new PricingPlanRule { PricingPlanId = weekendPlan.PricingPlanId, TableTypeId = tableTypes[0].TableTypeId, DayOfWeek = d, StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(13), HourlyRate = 50000, MinimumMinutes = 30, BillingBlockMinutes = 15 });
            rules.Add(new PricingPlanRule { PricingPlanId = weekendPlan.PricingPlanId, TableTypeId = tableTypes[2].TableTypeId, DayOfWeek = d, StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(13), HourlyRate = 60000, MinimumMinutes = 30, BillingBlockMinutes = 15 });
            rules.Add(new PricingPlanRule { PricingPlanId = weekendPlan.PricingPlanId, TableTypeId = tableTypes[1].TableTypeId, DayOfWeek = d, StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(13), HourlyRate = 80000, MinimumMinutes = 30, BillingBlockMinutes = 15 });
            rules.Add(new PricingPlanRule { PricingPlanId = weekendPlan.PricingPlanId, TableTypeId = tableTypes[3].TableTypeId, DayOfWeek = d, StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(13), HourlyRate = 90000, MinimumMinutes = 30, BillingBlockMinutes = 15 });
            
            // Ca Chiều
            rules.Add(new PricingPlanRule { PricingPlanId = weekendPlan.PricingPlanId, TableTypeId = tableTypes[0].TableTypeId, DayOfWeek = d, StartTime = TimeSpan.FromHours(13), EndTime = TimeSpan.FromHours(18), HourlyRate = 60000, MinimumMinutes = 30, BillingBlockMinutes = 15 });
            rules.Add(new PricingPlanRule { PricingPlanId = weekendPlan.PricingPlanId, TableTypeId = tableTypes[2].TableTypeId, DayOfWeek = d, StartTime = TimeSpan.FromHours(13), EndTime = TimeSpan.FromHours(18), HourlyRate = 70000, MinimumMinutes = 30, BillingBlockMinutes = 15 });
            rules.Add(new PricingPlanRule { PricingPlanId = weekendPlan.PricingPlanId, TableTypeId = tableTypes[1].TableTypeId, DayOfWeek = d, StartTime = TimeSpan.FromHours(13), EndTime = TimeSpan.FromHours(18), HourlyRate = 100000, MinimumMinutes = 30, BillingBlockMinutes = 15 });
            rules.Add(new PricingPlanRule { PricingPlanId = weekendPlan.PricingPlanId, TableTypeId = tableTypes[3].TableTypeId, DayOfWeek = d, StartTime = TimeSpan.FromHours(13), EndTime = TimeSpan.FromHours(18), HourlyRate = 100000, MinimumMinutes = 30, BillingBlockMinutes = 15 });
            
            // Ca Tối
            rules.Add(new PricingPlanRule { PricingPlanId = weekendPlan.PricingPlanId, TableTypeId = tableTypes[0].TableTypeId, DayOfWeek = d, StartTime = TimeSpan.FromHours(18), EndTime = new TimeSpan(23, 59, 59), HourlyRate = 70000, MinimumMinutes = 30, BillingBlockMinutes = 15 });
            rules.Add(new PricingPlanRule { PricingPlanId = weekendPlan.PricingPlanId, TableTypeId = tableTypes[2].TableTypeId, DayOfWeek = d, StartTime = TimeSpan.FromHours(18), EndTime = new TimeSpan(23, 59, 59), HourlyRate = 80000, MinimumMinutes = 30, BillingBlockMinutes = 15 });
            rules.Add(new PricingPlanRule { PricingPlanId = weekendPlan.PricingPlanId, TableTypeId = tableTypes[1].TableTypeId, DayOfWeek = d, StartTime = TimeSpan.FromHours(18), EndTime = new TimeSpan(23, 59, 59), HourlyRate = 120000, MinimumMinutes = 30, BillingBlockMinutes = 15 });
            rules.Add(new PricingPlanRule { PricingPlanId = weekendPlan.PricingPlanId, TableTypeId = tableTypes[3].TableTypeId, DayOfWeek = d, StartTime = TimeSpan.FromHours(18), EndTime = new TimeSpan(23, 59, 59), HourlyRate = 120000, MinimumMinutes = 30, BillingBlockMinutes = 15 });
        }
        db.PricingPlanRules.AddRange(rules);

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
                TableName = $"Bàn {i:00}",
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
        await EnsureDefaultPricingCoverageAsync(db, ct);

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

    private static async Task EnsureRolesAsync(PoolHubDbContext db, CancellationToken ct)
    {
        var definitions = new[]
        {
            new Role { Name = RoleConstants.Admin, Description = "Full system admin", IsSystem = true },
            new Role { Name = RoleConstants.Manager, Description = "Operations manager", IsSystem = true },
            new Role { Name = RoleConstants.Staff, Description = "Floor staff", IsSystem = true },
            new Role { Name = RoleConstants.Cashier, Description = "Cashier", IsSystem = true },
            new Role { Name = RoleConstants.Customer, Description = "Registered customer", IsSystem = true },
            new Role { Name = RoleConstants.Guest, Description = "Anonymous guest", IsSystem = true }
        };

        var existing = await db.Roles.Select(x => x.Name).ToListAsync(ct);
        var missing = definitions.Where(x => !existing.Contains(x.Name)).ToList();
        if (missing.Count == 0) return;

        db.Roles.AddRange(missing);
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureDemoUsersAsync(PoolHubDbContext db, CancellationToken ct)
    {
        var roleMap = await db.Roles.ToDictionaryAsync(x => x.Name, x => x.RoleId, ct);
        var demos = new[]
        {
            new { FullName = "Admin", Email = "admin@poolhub.com", Password = "Admin@123", Role = RoleConstants.Admin },
            new { FullName = "Manager", Email = "manager@poolhub.com", Password = "Manager@123", Role = RoleConstants.Manager },
            new { FullName = "Staff 1", Email = "staff1@poolhub.com", Password = "Staff@123", Role = RoleConstants.Staff },
            new { FullName = "Staff 2", Email = "staff2@poolhub.com", Password = "Staff@123", Role = RoleConstants.Staff },
            new { FullName = "Cashier", Email = "cashier@poolhub.com", Password = "Cashier@123", Role = RoleConstants.Cashier }
        };

        foreach (var demo in demos)
        {
            var email = demo.Email.Trim().ToLowerInvariant();
            var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email, ct);
            if (user is null)
            {
                user = new User
                {
                    FullName = demo.FullName,
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(demo.Password, 12),
                    EmailConfirmed = true,
                    Status = UserStatus.Active
                };
                db.Users.Add(user);
                await db.SaveChangesAsync(ct);
            }
            else
            {
                var changed = false;
                if (!string.Equals(user.Email, email, StringComparison.Ordinal))
                {
                    user.Email = email;
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(user.FullName))
                {
                    user.FullName = demo.FullName;
                    changed = true;
                }

                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    changed = true;
                }

                if (user.Status != UserStatus.Active)
                {
                    user.Status = UserStatus.Active;
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(demo.Password, user.PasswordHash))
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(demo.Password, 12);
                    changed = true;
                }

                if (changed)
                {
                    user.UpdatedAtUtc = DateTime.UtcNow;
                    await db.SaveChangesAsync(ct);
                }
            }

            var roleId = roleMap[demo.Role];
            var hasRole = await db.UserRoles.AnyAsync(x => x.UserId == user.UserId && x.RoleId == roleId, ct);
            if (!hasRole)
            {
                db.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = roleId });
                await db.SaveChangesAsync(ct);
            }
        }
    }

    private static async Task EnsureDemoVenueLayoutAsync(PoolHubDbContext db, CancellationToken ct)
    {
        var floors = await db.Floors.OrderBy(x => x.FloorId).ToListAsync(ct);
        if (floors.Count > 0)
        {
            floors[0].Name = "Tầng 1";
            floors[0].IsActive = true;
            floors[0].DisplayOrder = 1;
        }
        if (floors.Count > 1)
        {
            floors[1].Name = "Tầng 2 - VIP";
            floors[1].IsActive = true;
            floors[1].DisplayOrder = 2;
        }

        var zones = await db.Zones.OrderBy(x => x.ZoneId).ToListAsync(ct);
        var zoneNames = new[] { "Khu A", "Khu B", "Khu VIP", "Khu D - Snooker" };
        for (var index = 0; index < zones.Count && index < zoneNames.Length; index++)
        {
            zones[index].Name = zoneNames[index];
            zones[index].IsActive = true;
        }

        var tableNameByCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["A01"] = "Bàn A01",
            ["A02"] = "Bàn A02",
            ["B01"] = "Bàn B01 Carom",
            ["V01"] = "Bàn VIP 01",
            ["V02"] = "Bàn VIP 02"
        };

        var tables = await db.VenueTables.ToListAsync(ct);
        foreach (var table in tables)
        {
            if (tableNameByCode.TryGetValue(table.TableCode, out var name))
            {
                table.TableName = name;
            }
            else if (table.TableName.Contains("BÃ", StringComparison.OrdinalIgnoreCase))
            {
                table.TableName = table.TableCode.StartsWith("V", StringComparison.OrdinalIgnoreCase)
                    ? $"Bàn VIP {table.TableCode.TrimStart('V')}"
                    : $"Bàn {table.TableCode}";
            }

            table.IsActive = true;
            if (table.OperationalStatus is < 1 or > 5)
            {
                table.OperationalStatus = 1;
            }
        }

        await db.SaveChangesAsync(ct);
        await EnsureDemoCustomerReviewsAsync(db, ct);
    }

    private static async Task EnsureDefaultPricingCoverageAsync(PoolHubDbContext db, CancellationToken ct)
    {
        var activePlans = await db.PricingPlans
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.PricingPlanId)
            .ToListAsync(ct);
        var defaultPlan = activePlans.FirstOrDefault();

        if (defaultPlan is null)
        {
            defaultPlan = new PricingPlan
            {
                Name = "Default 2026",
                IsDefault = true,
                IsActive = true,
                StartsAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };
            db.PricingPlans.Add(defaultPlan);
            await db.SaveChangesAsync(ct);
            activePlans.Add(defaultPlan);
        }

        var activePlanIds = activePlans.Select(x => x.PricingPlanId).ToList();
        var tableTypes = await db.TableTypes
            .Where(x => x.IsActive)
            .OrderBy(x => x.TableTypeId)
            .ToListAsync(ct);
        var existingRules = await db.PricingPlanRules
            .Where(x => activePlanIds.Contains(x.PricingPlanId))
            .ToListAsync(ct);

        var startOfDay = TimeSpan.Zero;
        var firstDemoShift = TimeSpan.FromHours(8);
        var lastDemoShiftEnd = new TimeSpan(23, 59, 59);
        var endOfDay = TimeSpan.FromTicks(TimeSpan.TicksPerDay - 1);

        foreach (var tableType in tableTypes)
        {
            var fallbackRate = GetDefaultHourlyRate(tableType);
            for (var day = 0; day <= 6; day++)
            {
                var dayRules = existingRules
                    .Where(x => x.TableTypeId == tableType.TableTypeId && x.DayOfWeek == day && x.IsActive)
                    .ToList();

                if (dayRules.Count == 0)
                {
                    AddRuleIfMissing(defaultPlan.PricingPlanId, tableType.TableTypeId, day, startOfDay, endOfDay, fallbackRate);
                    continue;
                }

                foreach (var planRules in dayRules.GroupBy(x => x.PricingPlanId))
                {
                    var rate = planRules.OrderBy(x => x.StartTime).FirstOrDefault()?.HourlyRate ?? fallbackRate;
                    AddRuleIfMissing(planRules.Key, tableType.TableTypeId, day, startOfDay, firstDemoShift, rate);
                    AddRuleIfMissing(planRules.Key, tableType.TableTypeId, day, lastDemoShiftEnd, endOfDay, rate);
                }
            }
        }

        await db.SaveChangesAsync(ct);

        void AddRuleIfMissing(long pricingPlanId, long tableTypeId, int dayOfWeek, TimeSpan startTime, TimeSpan endTime, decimal hourlyRate)
        {
            var exists = existingRules.Any(x =>
                x.PricingPlanId == pricingPlanId &&
                x.TableTypeId == tableTypeId &&
                x.DayOfWeek == dayOfWeek &&
                x.StartTime == startTime);
            if (exists)
            {
                return;
            }

            var rule = new PricingPlanRule
            {
                PricingPlanId = pricingPlanId,
                TableTypeId = tableTypeId,
                DayOfWeek = dayOfWeek,
                StartTime = startTime,
                EndTime = endTime,
                HourlyRate = hourlyRate,
                MinimumMinutes = 30,
                BillingBlockMinutes = 15,
                IsActive = true
            };
            existingRules.Add(rule);
            db.PricingPlanRules.Add(rule);
        }
    }

    private static decimal GetDefaultHourlyRate(TableType tableType)
    {
        return tableType.Code.ToUpperInvariant() switch
        {
            "POOL_VIP" => 90000,
            "CAROM" => 60000,
            "SNOOKER" => 90000,
            _ => 50000
        };
    }

    private static async Task EnsureDemoCustomerReviewsAsync(PoolHubDbContext db, CancellationToken ct)
    {
        if (await db.CustomerReviews.AnyAsync(ct)) return;

        var customers = await db.Customers.OrderBy(x => x.CustomerId).Take(3).ToListAsync(ct);
        if (customers.Count == 0) return;

        var now = DateTime.UtcNow;
        var reviews = customers.Select((customer, index) => new CustomerReview
        {
            CustomerId = customer.CustomerId,
            Rating = index == 2 ? 4 : 5,
            Content = index switch
            {
                0 => "Đặt bàn nhanh, tới nơi có bàn sẵn và nhân viên hỗ trợ rất gọn.",
                1 => "Không gian sạch, đồ uống lên nhanh, nhóm mình chơi rất thoải mái.",
                _ => "Bàn VIP ổn, cơ gậy mới và thanh toán cuối ca rõ ràng."
            },
            DisplayName = customer.FullName,
            Status = CustomerReviewStatuses.Approved,
            IsFeatured = true,
            DisplayOrder = index + 1,
            Source = CustomerReviewSources.AdminImport,
            ApprovedAtUtc = now,
            CreatedAtUtc = now.AddDays(-(index + 1))
        }).ToList();

        db.CustomerReviews.AddRange(reviews);
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsurePermissionsAsync(PoolHubDbContext db, CancellationToken ct)
    {
        var definitions = PermissionConstants.All.Select(code =>
        {
            var group = code.Split('.')[0];
            return new Permission
            {
                Code = code,
                Name = code.Replace('.', ' '),
                Group = group,
                Description = $"Allows {code.Replace('.', ' ')} operations."
            };
        }).ToList();

        var existingCodes = await db.Permissions.Select(x => x.Code).ToListAsync(ct);
        var missing = definitions.Where(x => !existingCodes.Contains(x.Code)).ToList();
        if (missing.Count > 0)
        {
            db.Permissions.AddRange(missing);
            await db.SaveChangesAsync(ct);
        }

        var roleMap = await db.Roles.ToDictionaryAsync(x => x.Name, x => x.RoleId, ct);
        var permissionMap = await db.Permissions.Where(x => x.IsActive)
            .ToDictionaryAsync(x => x.Code, x => x.PermissionId, ct);
        var rolePermissions = new Dictionary<string, string[]>
        {
            [RoleConstants.Admin] = PermissionConstants.All,
            [RoleConstants.Manager] =
            [
                PermissionConstants.UsersManage, PermissionConstants.RolesManage,
                PermissionConstants.CustomersManage, PermissionConstants.VenueManage,
                PermissionConstants.PricingManage, PermissionConstants.ProductsManage,
                PermissionConstants.InventoryManage, PermissionConstants.ReportsView,
                PermissionConstants.AuditView
            ],
            [RoleConstants.Staff] = [PermissionConstants.CustomersManage],
            [RoleConstants.Cashier] = [PermissionConstants.DiscountsManage, PermissionConstants.PaymentsManage]
        };

        foreach (var (roleName, codes) in rolePermissions)
        {
            if (!roleMap.TryGetValue(roleName, out var roleId)) continue;
            foreach (var code in codes)
            {
                var permissionId = permissionMap[code];
                if (!await db.RolePermissions.AnyAsync(
                    x => x.RoleId == roleId && x.PermissionId == permissionId, ct))
                {
                    db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureDemoDiscountsAsync(PoolHubDbContext db, CancellationToken ct)
    {
        if (await db.Discounts.AnyAsync(ct)) return;
        var now = DateTime.UtcNow;
        var discounts = new[]
        {
            new Discount { DiscountCode = "DISCOUNT10", Name = "Giảm 10% tiền giờ chơi", DiscountType = "PERCENTAGE", Value = 10, AppliesTo = "TIME", StartsAtUtc = now.AddDays(-10), EndsAtUtc = now.AddYears(1), IsActive = true },
            new Discount { DiscountCode = "POOLVIP20", Name = "Giảm 20% tổng hóa đơn", DiscountType = "PERCENTAGE", Value = 20, AppliesTo = "ALL", StartsAtUtc = now.AddDays(-5), EndsAtUtc = now.AddYears(1), IsActive = true, MaxAmount = 50000 },
            new Discount { DiscountCode = "FIXED50K", Name = "Giảm trực tiếp 50K", DiscountType = "FIXED", Value = 50000, AppliesTo = "ALL", StartsAtUtc = now.AddDays(-10), EndsAtUtc = now.AddYears(1), IsActive = true, MinTimeSubtotal = 100000 }
        };
        db.Discounts.AddRange(discounts);
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureDepositPaymentMethodAsync(PoolHubDbContext db, CancellationToken ct)
    {
        if (await db.PaymentMethods.AnyAsync(x => x.Code == "DEPOSIT", ct)) return;

        db.PaymentMethods.Add(new PaymentMethod
        {
            Name = "Deposit Applied",
            Code = "DEPOSIT",
            Description = "System payment method used when applying booking deposits to invoices.",
            IsActive = true
        });
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
