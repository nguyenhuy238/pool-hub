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
            new PaymentMethod { Name = "EWallet", Code = "EWALLET" }
        );

        await db.SaveChangesAsync(ct);

        var tableTypes = await db.TableTypes.OrderBy(x => x.TableTypeId).ToListAsync(ct);
        var zones = await db.Zones.OrderBy(x => x.ZoneId).ToListAsync(ct);
        var categories = await db.ProductCategories.OrderBy(x => x.ProductCategoryId).ToListAsync(ct);

        db.PricingPlanRules.AddRange(
            new PricingPlanRule { PricingPlanId = plan.PricingPlanId, TableTypeId = tableTypes[0].TableTypeId, DayType = 1, StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(17), HourlyRate = 50000, MinimumMinutes = 0, BillingBlockMinutes = 1 },
            new PricingPlanRule { PricingPlanId = plan.PricingPlanId, TableTypeId = tableTypes[1].TableTypeId, DayType = 1, StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(17), HourlyRate = 90000, MinimumMinutes = 0, BillingBlockMinutes = 1 },
            new PricingPlanRule { PricingPlanId = plan.PricingPlanId, TableTypeId = tableTypes[2].TableTypeId, DayType = 1, StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(17), HourlyRate = 60000, MinimumMinutes = 0, BillingBlockMinutes = 1 }
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

    private static async Task EnsureRolesAsync(PoolHubDbContext db, CancellationToken ct)
    {
        var definitions = new[]
        {
            new Role { Name = RoleConstants.Admin, Description = "Full system admin", IsSystem = true },
            new Role { Name = RoleConstants.Manager, Description = "Operations manager", IsSystem = true },
            new Role { Name = RoleConstants.Staff, Description = "Floor staff", IsSystem = true },
            new Role { Name = RoleConstants.Customer, Description = "Registered customer", IsSystem = true },
            new Role { Name = RoleConstants.Guest, Description = "Anonymous guest", IsSystem = true }
        };

        var existing = await db.Roles.Select(x => x.Name.ToLower()).ToListAsync(ct);
        var missing = definitions.Where(x => !existing.Contains(x.Name.ToLowerInvariant())).ToList();
        if (missing.Count == 0) return;

        db.Roles.AddRange(missing);
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
                PermissionConstants.CustomersManage, PermissionConstants.VenueManage,
                PermissionConstants.PricingManage, PermissionConstants.ProductsManage,
                PermissionConstants.InventoryManage, PermissionConstants.DiscountsManage,
                PermissionConstants.PaymentsManage, PermissionConstants.LandingManage,
                PermissionConstants.ReportsView
            ],
            [RoleConstants.Staff] =
            [
                PermissionConstants.CustomersManage,
                PermissionConstants.DiscountsManage,
                PermissionConstants.PaymentsManage
            ]
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

    private static async Task EnsureDemoUsersAsync(PoolHubDbContext db, CancellationToken ct)
    {
        var roleMap = await db.Roles.ToDictionaryAsync(x => x.Name, x => x.RoleId, ct);
        var demos = new[]
        {
            new { FullName = "Admin", Email = "admin@poolhub.com", Password = "Admin@123", Role = RoleConstants.Admin },
            new { FullName = "Manager", Email = "manager@poolhub.com", Password = "Manager@123", Role = RoleConstants.Manager },
            new { FullName = "Staff 1", Email = "staff1@poolhub.com", Password = "Staff@123", Role = RoleConstants.Staff },
            new { FullName = "Staff 2", Email = "staff2@poolhub.com", Password = "Staff@123", Role = RoleConstants.Staff }
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

    private static async Task EnsureDemoDiscountsAsync(PoolHubDbContext db, CancellationToken ct)
    {
        if (await db.Discounts.AnyAsync(ct)) return;
        var now = DateTime.UtcNow;
        var discounts = new[]
        {
            new Discount { DiscountCode = "DISCOUNT10", Name = "10% off table time", DiscountType = "PERCENTAGE", Value = 10, AppliesTo = "TIME", StartsAtUtc = now.AddDays(-10), EndsAtUtc = now.AddYears(1), IsActive = true },
            new Discount { DiscountCode = "POOLVIP20", Name = "20% off total bill", DiscountType = "PERCENTAGE", Value = 20, AppliesTo = "ALL", StartsAtUtc = now.AddDays(-5), EndsAtUtc = now.AddYears(1), IsActive = true, MaxAmount = 50000 },
            new Discount { DiscountCode = "FIXED50K", Name = "50K fixed discount", DiscountType = "FIXED", Value = 50000, AppliesTo = "ALL", StartsAtUtc = now.AddDays(-10), EndsAtUtc = now.AddYears(1), IsActive = true, MinTimeSubtotal = 100000 }
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

    private static async Task EnsureDemoVenueLayoutAsync(PoolHubDbContext db, CancellationToken ct)
    {
        var floors = await db.Floors.OrderBy(x => x.FloorId).ToListAsync(ct);
        if (floors.Count > 0)
        {
            floors[0].Name = "Floor 1";
            floors[0].IsActive = true;
            floors[0].DisplayOrder = 1;
        }
        if (floors.Count > 1)
        {
            floors[1].Name = "Floor 2 - VIP";
            floors[1].IsActive = true;
            floors[1].DisplayOrder = 2;
        }

        var zones = await db.Zones.OrderBy(x => x.ZoneId).ToListAsync(ct);
        var zoneNames = new[] { "Zone A", "Zone B", "VIP Zone", "Zone D - Snooker" };
        for (var index = 0; index < zones.Count && index < zoneNames.Length; index++)
        {
            zones[index].Name = zoneNames[index];
            zones[index].IsActive = true;
        }

        var tableNameByCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["A01"] = "Table A01",
            ["A02"] = "Table A02",
            ["B01"] = "Table B01 Carom",
            ["V01"] = "VIP Table 01",
            ["V02"] = "VIP Table 02"
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
                    ? $"VIP Table {table.TableCode.TrimStart('V')}"
                    : $"Table {table.TableCode}";
            }

            table.IsActive = true;
            if (table.OperationalStatus is < 1 or > 5)
            {
                table.OperationalStatus = 1;
            }
        }

        await db.SaveChangesAsync(ct);
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
        var endOfDay = new TimeSpan(23, 59, 59);

        // Map rates for table type code to (StartTime, EndTime, Rate) for Day 1
        var day1Rates = new Dictionary<string, (TimeSpan start, TimeSpan end, decimal rate)[]>
        {
            { "POOL_STD", new[] {
                (startOfDay, new TimeSpan(13, 0, 0), 40000m),
                (new TimeSpan(13, 0, 0), new TimeSpan(18, 0, 0), 50000m),
                (new TimeSpan(18, 0, 0), endOfDay, 60000m)
            }},
            { "POOL_VIP", new[] {
                (startOfDay, new TimeSpan(13, 0, 0), 70000m),
                (new TimeSpan(13, 0, 0), new TimeSpan(18, 0, 0), 90000m),
                (new TimeSpan(18, 0, 0), endOfDay, 100000m)
            }},
            { "CAROM", new[] {
                (startOfDay, new TimeSpan(13, 0, 0), 50000m),
                (new TimeSpan(13, 0, 0), new TimeSpan(18, 0, 0), 60000m),
                (new TimeSpan(18, 0, 0), endOfDay, 70000m)
            }},
            { "SNOOKER", new[] {
                (startOfDay, new TimeSpan(13, 0, 0), 80000m),
                (new TimeSpan(13, 0, 0), new TimeSpan(18, 0, 0), 90000m),
                (new TimeSpan(18, 0, 0), endOfDay, 110000m)
            }}
        };

        // Day 2 rates (flat rate for entire day)
        var day2Rates = new Dictionary<string, decimal>
        {
            { "POOL_STD", 70000m },
            { "POOL_VIP", 110000m },
            { "CAROM", 80000m },
            { "SNOOKER", 120000m }
        };

        foreach (var tableType in tableTypes)
        {
            var code = tableType.Code.ToUpperInvariant();
            
            // Day 1 (Weekday)
            if (day1Rates.TryGetValue(code, out var shifts))
            {
                foreach (var shift in shifts)
                {
                    AddRuleIfMissing(defaultPlan.PricingPlanId, tableType.TableTypeId, 1, shift.start, shift.end, shift.rate);
                }
            }
            else
            {
                // Fallback for unknown table types
                AddRuleIfMissing(defaultPlan.PricingPlanId, tableType.TableTypeId, 1, startOfDay, endOfDay, 50000m);
            }

            // Day 2 (Weekend/Holiday)
            if (day2Rates.TryGetValue(code, out var d2Rate))
            {
                AddRuleIfMissing(defaultPlan.PricingPlanId, tableType.TableTypeId, 2, startOfDay, endOfDay, d2Rate);
            }
            else
            {
                AddRuleIfMissing(defaultPlan.PricingPlanId, tableType.TableTypeId, 2, startOfDay, endOfDay, 50000m);
            }
        }

        await db.SaveChangesAsync(ct);

        void AddRuleIfMissing(long pricingPlanId, long tableTypeId, int dayType, TimeSpan startTime, TimeSpan endTime, decimal hourlyRate)
        {
            var exists = existingRules.Any(x =>
                x.PricingPlanId == pricingPlanId &&
                x.TableTypeId == tableTypeId &&
                x.DayType == dayType &&
                x.StartTime == startTime);
            if (exists)
            {
                return;
            }

            var rule = new PricingPlanRule
            {
                PricingPlanId = pricingPlanId,
                TableTypeId = tableTypeId,
                DayType = dayType,
                StartTime = startTime,
                EndTime = endTime,
                HourlyRate = hourlyRate,
                MinimumMinutes = 0,
                BillingBlockMinutes = 1,
                IsActive = true
            };
            existingRules.Add(rule);
            db.PricingPlanRules.Add(rule);
        }
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
                0 => "Fast booking and helpful staff.",
                1 => "Clean space, quick drinks, and a comfortable session.",
                _ => "VIP table was good and checkout was clear."
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
