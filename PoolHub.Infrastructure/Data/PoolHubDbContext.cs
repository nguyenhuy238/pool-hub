using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;
using PoolHub.Core.Entities;
using PoolHub.Shared.Time;

namespace PoolHub.Infrastructure.Data;

public class PoolHubDbContext(
    DbContextOptions<PoolHubDbContext> options,
    IHttpContextAccessor? httpContextAccessor = null,
    IClock? clock = null) : DbContext(options)
{
    private readonly IClock _clock = clock ?? SystemClock.Instance;
    private static readonly HashSet<string> ExplicitlyAuditedEntities =
    [
        nameof(AuditLog), nameof(User), nameof(Role), nameof(Customer), nameof(RefreshToken),
        nameof(PasswordResetToken), nameof(SiteSetting), nameof(MediaAsset), nameof(Discount), nameof(CustomerReview), nameof(CustomerReviewInvitation),
        nameof(PaymentMethod), nameof(Permission), nameof(RolePermission), nameof(CustomerPointHistory)
    ];

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerPointHistory> CustomerPointHistories => Set<CustomerPointHistory>();
    public DbSet<Floor> Floors => Set<Floor>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<TableType> TableTypes => Set<TableType>();
    public DbSet<VenueTable> VenueTables => Set<VenueTable>();
    public DbSet<PricingPlan> PricingPlans => Set<PricingPlan>();
    public DbSet<PricingPlanRule> PricingPlanRules => Set<PricingPlanRule>();
    public DbSet<PricingSpecialDate> PricingSpecialDates => Set<PricingSpecialDate>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingDeposit> BookingDeposits => Set<BookingDeposit>();
    public DbSet<BookingTable> BookingTables => Set<BookingTable>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<SessionTableAssignment> SessionTableAssignments => Set<SessionTableAssignment>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Discount> Discounts => Set<Discount>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<InvoiceDiscount> InvoiceDiscounts => Set<InvoiceDiscount>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<CustomerReview> CustomerReviews => Set<CustomerReview>();
    public DbSet<CustomerReviewInvitation> CustomerReviewInvitations => Set<CustomerReviewInvitation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PoolHubDbContext).Assembly);
        ApplySnakeCaseColumns(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyUtcTimestamps();
        AddAutomaticAuditEntries();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyUtcTimestamps();
        AddAutomaticAuditEntries();
        return base.SaveChanges();
    }

    private void ApplyUtcTimestamps()
    {
        var now = _clock.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.CreatedAtUtc == default)
            {
                entry.Entity.CreatedAtUtc = now;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<UserRole>().Where(x => x.State == EntityState.Added && x.Entity.AssignedAtUtc == default))
        {
            entry.Entity.AssignedAtUtc = now;
        }

        foreach (var entry in ChangeTracker.Entries<RolePermission>().Where(x => x.State == EntityState.Added && x.Entity.AssignedAtUtc == default))
        {
            entry.Entity.AssignedAtUtc = now;
        }

        foreach (var entry in ChangeTracker.Entries<PricingPlan>().Where(x => x.State == EntityState.Added && x.Entity.StartsAtUtc == default))
        {
            entry.Entity.StartsAtUtc = now;
        }
    }

    private void AddAutomaticAuditEntries()
    {
        var context = httpContextAccessor?.HttpContext;
        if (context is null) return;

        var actorId = long.TryParse(
            context.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            context.User.FindFirstValue("sub"), out var parsedActorId)
            ? parsedActorId
            : (long?)null;

        var mutations = ChangeTracker.Entries()
            .Where(entry => entry.Entity is BaseEntity &&
                            entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted &&
                            !ExplicitlyAuditedEntities.Contains(entry.Metadata.ClrType.Name))
            .ToList();

        foreach (var entry in mutations)
        {
            var action = entry.State switch
            {
                EntityState.Added => "CREATE",
                EntityState.Modified => "UPDATE",
                EntityState.Deleted => "DELETE",
                _ => "MUTATE"
            };
            var entityName = entry.Metadata.ClrType.Name;
            var primaryKey = entry.Properties.FirstOrDefault(x => x.Metadata.IsPrimaryKey());
            var publicId = entry.Properties.FirstOrDefault(x => x.Metadata.Name == "PublicId")?.CurrentValue as Guid?;
            var oldValues = entry.State == EntityState.Added ? null : SerializeProperties(entry, original: true);
            var newValues = entry.State == EntityState.Deleted ? null : SerializeProperties(entry, original: false);

            AuditLogs.Add(new AuditLog
            {
                ActorUserId = actorId,
                Action = $"{entityName.ToUpperInvariant()}_{action}",
                EntityName = entityName,
                EntityId = primaryKey?.CurrentValue is null ? null : Convert.ToInt64(primaryKey.CurrentValue),
                EntityPublicId = publicId,
                OldValues = oldValues,
                NewValues = newValues,
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers.UserAgent.ToString(),
                Description = $"Automatic audit for {action.ToLowerInvariant()} {entityName}."
            });
        }
    }

    private static string SerializeProperties(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, bool original)
    {
        var values = entry.Properties
            .Where(property => property.Metadata.Name is not "PasswordHash" and not "TokenHash" and not "RowVersion")
            .ToDictionary(
                property => property.Metadata.Name,
                property => original ? property.OriginalValue : property.CurrentValue);
        return JsonSerializer.Serialize(values);
    }

    private static void ApplySnakeCaseColumns(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }
        }
    }

    private static string ToSnakeCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;

        var chars = new List<char>(value.Length + 8);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsUpper(c))
            {
                if (i > 0 && (char.IsLower(value[i - 1]) || char.IsDigit(value[i - 1]) || (i + 1 < value.Length && char.IsLower(value[i + 1]))))
                {
                    chars.Add('_');
                }

                chars.Add(char.ToLowerInvariant(c));
            }
            else
            {
                chars.Add(c);
            }
        }

        return new string(chars.ToArray());
    }
}
