using Microsoft.EntityFrameworkCore;
using PoolHub.Core.Entities;

namespace PoolHub.Infrastructure.Data;

public class PoolHubDbContext(DbContextOptions<PoolHubDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Floor> Floors => Set<Floor>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<TableType> TableTypes => Set<TableType>();
    public DbSet<VenueTable> VenueTables => Set<VenueTable>();
    public DbSet<PricingPlan> PricingPlans => Set<PricingPlan>();
    public DbSet<PricingPlanRule> PricingPlanRules => Set<PricingPlanRule>();
    public DbSet<Booking> Bookings => Set<Booking>();
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureTables(modelBuilder);
        ConfigureKeysAndIndexes(modelBuilder);
        ConfigureRelationships(modelBuilder);
        ConfigurePrecision(modelBuilder);

        modelBuilder.Entity<User>().Property(x => x.RowVersion).IsRowVersion().HasColumnName("row_version");
        modelBuilder.Entity<Product>().Property(x => x.RowVersion).IsRowVersion().HasColumnName("row_version");

        ApplySnakeCaseColumns(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    private static void ConfigureTables(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().ToTable("roles");
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<UserRole>().ToTable("user_roles");
        modelBuilder.Entity<RefreshToken>().ToTable("refresh_tokens");
        modelBuilder.Entity<Customer>().ToTable("customers");
        modelBuilder.Entity<Floor>().ToTable("floors");
        modelBuilder.Entity<Zone>().ToTable("zones");
        modelBuilder.Entity<TableType>().ToTable("table_types");
        modelBuilder.Entity<VenueTable>().ToTable("venue_tables");
        modelBuilder.Entity<PricingPlan>().ToTable("pricing_plans");
        modelBuilder.Entity<PricingPlanRule>().ToTable("pricing_plan_rules");
        modelBuilder.Entity<Booking>().ToTable("bookings");
        modelBuilder.Entity<Session>().ToTable("sessions");
        modelBuilder.Entity<SessionTableAssignment>().ToTable("session_table_assignments");
        modelBuilder.Entity<ProductCategory>().ToTable("product_categories");
        modelBuilder.Entity<Product>().ToTable("products");
        modelBuilder.Entity<InventoryTransaction>().ToTable("inventory_transactions");
        modelBuilder.Entity<Order>().ToTable("orders");
        modelBuilder.Entity<OrderItem>().ToTable("order_items");
        modelBuilder.Entity<Discount>().ToTable("discounts");
        modelBuilder.Entity<Invoice>().ToTable("invoices");
        modelBuilder.Entity<InvoiceLine>().ToTable("invoice_lines");
        modelBuilder.Entity<InvoiceDiscount>().ToTable("invoice_discounts");
        modelBuilder.Entity<PaymentMethod>().ToTable("payment_methods");
        modelBuilder.Entity<Payment>().ToTable("payments");
        modelBuilder.Entity<Notification>().ToTable("notifications");
        modelBuilder.Entity<AuditLog>().ToTable("audit_logs");
    }

    private static void ConfigureKeysAndIndexes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasKey(x => x.RoleId);
        modelBuilder.Entity<User>().HasKey(x => x.UserId);
        modelBuilder.Entity<UserRole>().HasKey(x => new { x.UserId, x.RoleId });
        modelBuilder.Entity<RefreshToken>().HasKey(x => x.RefreshTokenId);
        modelBuilder.Entity<Customer>().HasKey(x => x.CustomerId);
        modelBuilder.Entity<Floor>().HasKey(x => x.FloorId);
        modelBuilder.Entity<Zone>().HasKey(x => x.ZoneId);
        modelBuilder.Entity<TableType>().HasKey(x => x.TableTypeId);
        modelBuilder.Entity<VenueTable>().HasKey(x => x.TableId);
        modelBuilder.Entity<PricingPlan>().HasKey(x => x.PricingPlanId);
        modelBuilder.Entity<PricingPlanRule>().HasKey(x => x.PricingPlanRuleId);
        modelBuilder.Entity<Booking>().HasKey(x => x.BookingId);
        modelBuilder.Entity<Session>().HasKey(x => x.SessionId);
        modelBuilder.Entity<SessionTableAssignment>().HasKey(x => x.SessionTableAssignmentId);
        modelBuilder.Entity<ProductCategory>().HasKey(x => x.ProductCategoryId);
        modelBuilder.Entity<Product>().HasKey(x => x.ProductId);
        modelBuilder.Entity<InventoryTransaction>().HasKey(x => x.InventoryTransactionId);
        modelBuilder.Entity<Order>().HasKey(x => x.OrderId);
        modelBuilder.Entity<OrderItem>().HasKey(x => x.OrderItemId);
        modelBuilder.Entity<Discount>().HasKey(x => x.DiscountId);
        modelBuilder.Entity<Invoice>().HasKey(x => x.InvoiceId);
        modelBuilder.Entity<InvoiceLine>().HasKey(x => x.InvoiceLineId);
        modelBuilder.Entity<InvoiceDiscount>().HasKey(x => x.InvoiceDiscountId);
        modelBuilder.Entity<PaymentMethod>().HasKey(x => x.PaymentMethodId);
        modelBuilder.Entity<Payment>().HasKey(x => x.PaymentId);
        modelBuilder.Entity<Notification>().HasKey(x => x.NotificationId);
        modelBuilder.Entity<AuditLog>().HasKey(x => x.AuditLogId);

        modelBuilder.Entity<Role>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<User>().HasIndex(x => x.PublicId).IsUnique();
        modelBuilder.Entity<Customer>().HasIndex(x => x.PublicId).IsUnique();
        modelBuilder.Entity<TableType>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<VenueTable>().HasIndex(x => x.PublicId).IsUnique();
        modelBuilder.Entity<VenueTable>().HasIndex(x => x.TableCode).IsUnique();
        modelBuilder.Entity<PricingPlanRule>().HasIndex(x => new { x.PricingPlanId, x.TableTypeId, x.DayOfWeek, x.StartTime }).IsUnique();
        modelBuilder.Entity<Booking>().HasIndex(x => x.PublicId).IsUnique();
        modelBuilder.Entity<Booking>().HasIndex(x => x.BookingCode).IsUnique();
        modelBuilder.Entity<Session>().HasIndex(x => x.PublicId).IsUnique();
        modelBuilder.Entity<Session>().HasIndex(x => x.SessionCode).IsUnique();
        modelBuilder.Entity<Session>().HasIndex(x => x.BookingId).IsUnique().HasFilter("[booking_id] IS NOT NULL");
        modelBuilder.Entity<Product>().HasIndex(x => x.PublicId).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(x => x.Sku).IsUnique();
        modelBuilder.Entity<Order>().HasIndex(x => x.PublicId).IsUnique();
        modelBuilder.Entity<Order>().HasIndex(x => x.OrderCode).IsUnique();
        modelBuilder.Entity<Discount>().HasIndex(x => x.DiscountCode).IsUnique();
        modelBuilder.Entity<Invoice>().HasIndex(x => x.PublicId).IsUnique();
        modelBuilder.Entity<Invoice>().HasIndex(x => x.InvoiceCode).IsUnique();
        modelBuilder.Entity<Invoice>().HasIndex(x => x.SessionId).IsUnique();
        modelBuilder.Entity<Payment>().HasIndex(x => x.PublicId).IsUnique();
        modelBuilder.Entity<PaymentMethod>().HasIndex(x => x.Code).IsUnique();
    }

    private static void ConfigureRelationships(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserRole>().HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<UserRole>().HasOne<Role>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<UserRole>().HasOne<User>().WithMany().HasForeignKey(x => x.AssignedByUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<RefreshToken>().HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Zone>().HasOne<Floor>().WithMany().HasForeignKey(x => x.FloorId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<VenueTable>().HasOne<Zone>().WithMany().HasForeignKey(x => x.ZoneId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<VenueTable>().HasOne<TableType>().WithMany().HasForeignKey(x => x.TableTypeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PricingPlanRule>().HasOne<PricingPlan>().WithMany().HasForeignKey(x => x.PricingPlanId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PricingPlanRule>().HasOne<TableType>().WithMany().HasForeignKey(x => x.TableTypeId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Booking>().HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Booking>().HasOne<VenueTable>().WithMany().HasForeignKey(x => x.TableId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Booking>().HasOne<TableType>().WithMany().HasForeignKey(x => x.TableTypeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Booking>().HasOne<User>().WithMany().HasForeignKey(x => x.ConfirmedByUserId).OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Session>().HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Session>().HasOne<Booking>().WithOne().HasForeignKey<Session>(x => x.BookingId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Session>().HasOne<User>().WithMany().HasForeignKey(x => x.OpenedByUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Session>().HasOne<User>().WithMany().HasForeignKey(x => x.ClosedByUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<SessionTableAssignment>().HasOne<Session>().WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SessionTableAssignment>().HasOne<VenueTable>().WithMany().HasForeignKey(x => x.TableId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SessionTableAssignment>().HasOne<PricingPlanRule>().WithMany().HasForeignKey(x => x.PricingPlanRuleId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SessionTableAssignment>().HasOne<User>().WithMany().HasForeignKey(x => x.AssignedByUserId).OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Product>().HasOne<ProductCategory>().WithMany().HasForeignKey(x => x.ProductCategoryId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<InventoryTransaction>().HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<InventoryTransaction>().HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Order>().HasOne<Session>().WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Order>().HasOne<User>().WithMany().HasForeignKey(x => x.OrderedByUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<OrderItem>().HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<OrderItem>().HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Invoice>().HasOne<Session>().WithOne().HasForeignKey<Invoice>(x => x.SessionId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Invoice>().HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Invoice>().HasOne<User>().WithMany().HasForeignKey(x => x.IssuedByUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<InvoiceLine>().HasOne<Invoice>().WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<InvoiceDiscount>().HasOne<Invoice>().WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<InvoiceDiscount>().HasOne<Discount>().WithMany().HasForeignKey(x => x.DiscountId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<InvoiceDiscount>().HasOne<User>().WithMany().HasForeignKey(x => x.AppliedByUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Payment>().HasOne<Invoice>().WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Payment>().HasOne<PaymentMethod>().WithMany().HasForeignKey(x => x.PaymentMethodId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Payment>().HasOne<User>().WithMany().HasForeignKey(x => x.ReceivedByUserId).OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Notification>().HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Notification>().HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<AuditLog>().HasOne<User>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.NoAction);
    }

    private static void ConfigurePrecision(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VenueTable>().Property(x => x.PositionX).HasPrecision(10, 2);
        modelBuilder.Entity<VenueTable>().Property(x => x.PositionY).HasPrecision(10, 2);

        modelBuilder.Entity<PricingPlanRule>().Property(x => x.HourlyRate).HasPrecision(19, 4);
        modelBuilder.Entity<SessionTableAssignment>().Property(x => x.HourlyRateSnapshot).HasPrecision(19, 4);
        modelBuilder.Entity<SessionTableAssignment>().Property(x => x.Amount).HasPrecision(19, 4);
        modelBuilder.Entity<Product>().Property(x => x.UnitPrice).HasPrecision(19, 4);
        modelBuilder.Entity<InventoryTransaction>().Property(x => x.UnitCost).HasPrecision(19, 4);
        modelBuilder.Entity<Order>().Property(x => x.SubtotalAmount).HasPrecision(19, 4);
        modelBuilder.Entity<OrderItem>().Property(x => x.UnitPriceSnapshot).HasPrecision(19, 4);
        modelBuilder.Entity<OrderItem>().Property(x => x.LineTotalAmount).HasPrecision(19, 4);
        modelBuilder.Entity<Discount>().Property(x => x.Value).HasPrecision(19, 4);
        modelBuilder.Entity<Discount>().Property(x => x.MaxAmount).HasPrecision(19, 4);
        modelBuilder.Entity<Discount>().Property(x => x.MinTimeSubtotal).HasPrecision(19, 4);
        modelBuilder.Entity<Invoice>().Property(x => x.TimeSubtotalAmount).HasPrecision(19, 4);
        modelBuilder.Entity<Invoice>().Property(x => x.ProductSubtotalAmount).HasPrecision(19, 4);
        modelBuilder.Entity<Invoice>().Property(x => x.SubtotalAmount).HasPrecision(19, 4);
        modelBuilder.Entity<Invoice>().Property(x => x.DiscountAmount).HasPrecision(19, 4);
        modelBuilder.Entity<Invoice>().Property(x => x.TaxAmount).HasPrecision(19, 4);
        modelBuilder.Entity<Invoice>().Property(x => x.GrandTotalAmount).HasPrecision(19, 4);
        modelBuilder.Entity<Invoice>().Property(x => x.PaidAmount).HasPrecision(19, 4);
        modelBuilder.Entity<InvoiceLine>().Property(x => x.Quantity).HasPrecision(19, 4);
        modelBuilder.Entity<InvoiceLine>().Property(x => x.UnitPrice).HasPrecision(19, 4);
        modelBuilder.Entity<InvoiceLine>().Property(x => x.LineTotalAmount).HasPrecision(19, 4);
        modelBuilder.Entity<InvoiceDiscount>().Property(x => x.AmountApplied).HasPrecision(19, 4);
        modelBuilder.Entity<Payment>().Property(x => x.Amount).HasPrecision(19, 4);
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
