using Microsoft.EntityFrameworkCore;
using PoolHub.Core.Entities;

namespace PoolHub.Infrastructure.Data;

public class PoolHubDbContext(DbContextOptions<PoolHubDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>(); public DbSet<User> Users => Set<User>(); public DbSet<UserRole> UserRoles => Set<UserRole>(); public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>(); public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Floor> Floors => Set<Floor>(); public DbSet<Zone> Zones => Set<Zone>(); public DbSet<TableType> TableTypes => Set<TableType>(); public DbSet<VenueTable> VenueTables => Set<VenueTable>(); public DbSet<PricingPlan> PricingPlans => Set<PricingPlan>();
    public DbSet<PricingPlanRule> PricingPlanRules => Set<PricingPlanRule>(); public DbSet<Booking> Bookings => Set<Booking>(); public DbSet<Session> Sessions => Set<Session>(); public DbSet<SessionTableAssignment> SessionTableAssignments => Set<SessionTableAssignment>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>(); public DbSet<Product> Products => Set<Product>(); public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>(); public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>(); public DbSet<Discount> Discounts => Set<Discount>(); public DbSet<Invoice> Invoices => Set<Invoice>(); public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<InvoiceDiscount> InvoiceDiscounts => Set<InvoiceDiscount>(); public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>(); public DbSet<Payment> Payments => Set<Payment>(); public DbSet<Notification> Notifications => Set<Notification>(); public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserRole>().HasKey(x => new { x.UserId, x.RoleId });
        modelBuilder.Entity<VenueTable>().HasKey(x => x.TableId);
        modelBuilder.Entity<SessionTableAssignment>().HasKey(x => x.AssignmentId);
        modelBuilder.Entity<ProductCategory>().HasKey(x => x.CategoryId);

        modelBuilder.Entity<User>().Property(x => x.RowVersion).IsRowVersion();
        modelBuilder.Entity<Product>().Property(x => x.RowVersion).IsRowVersion();

        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique(); modelBuilder.Entity<User>().HasIndex(x => x.PublicId).IsUnique(); modelBuilder.Entity<Role>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<TableType>().HasIndex(x => x.Code).IsUnique(); modelBuilder.Entity<VenueTable>().HasIndex(x => x.TableCode).IsUnique(); modelBuilder.Entity<VenueTable>().HasIndex(x => x.PublicId).IsUnique();
        modelBuilder.Entity<ProductCategory>().HasIndex(x => x.Code).IsUnique(); modelBuilder.Entity<Product>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Booking>().HasIndex(x => x.BookingCode).IsUnique(); modelBuilder.Entity<Session>().HasIndex(x => x.SessionCode).IsUnique(); modelBuilder.Entity<Invoice>().HasIndex(x => x.InvoiceCode).IsUnique();

        modelBuilder.Entity<Zone>().HasOne<Floor>().WithMany().HasForeignKey(x => x.FloorId);
        modelBuilder.Entity<VenueTable>().HasOne<Zone>().WithMany().HasForeignKey(x => x.ZoneId);
        modelBuilder.Entity<VenueTable>().HasOne<TableType>().WithMany().HasForeignKey(x => x.TableTypeId);
        modelBuilder.Entity<PricingPlanRule>().HasOne<PricingPlan>().WithMany().HasForeignKey(x => x.PricingPlanId);
        modelBuilder.Entity<PricingPlanRule>().HasOne<TableType>().WithMany().HasForeignKey(x => x.TableTypeId);
        modelBuilder.Entity<Booking>().HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Booking>().HasOne<VenueTable>().WithMany().HasForeignKey(x => x.TableId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Booking>().HasOne<TableType>().WithMany().HasForeignKey(x => x.TableTypeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SessionTableAssignment>().HasOne<Session>().WithMany().HasForeignKey(x => x.SessionId);
        modelBuilder.Entity<SessionTableAssignment>().HasOne<VenueTable>().WithMany().HasForeignKey(x => x.TableId);
        modelBuilder.Entity<Product>().HasOne<ProductCategory>().WithMany().HasForeignKey(x => x.CategoryId);

        modelBuilder.Entity<Discount>().Property(x => x.Value).HasPrecision(18, 2);
        modelBuilder.Entity<Invoice>().Property(x => x.SubtotalAmount).HasPrecision(18, 2);
        modelBuilder.Entity<Invoice>().Property(x => x.DiscountAmount).HasPrecision(18, 2);
        modelBuilder.Entity<Invoice>().Property(x => x.FinalAmount).HasPrecision(18, 2);
        modelBuilder.Entity<InvoiceDiscount>().Property(x => x.DiscountAmount).HasPrecision(18, 2);
        modelBuilder.Entity<InvoiceLine>().Property(x => x.UnitPrice).HasPrecision(18, 2);
        modelBuilder.Entity<InvoiceLine>().Property(x => x.LineTotal).HasPrecision(18, 2);
        modelBuilder.Entity<OrderItem>().Property(x => x.UnitPrice).HasPrecision(18, 2);
        modelBuilder.Entity<OrderItem>().Property(x => x.LineTotal).HasPrecision(18, 2);
        modelBuilder.Entity<Payment>().Property(x => x.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<PricingPlanRule>().Property(x => x.HourlyRate).HasPrecision(18, 2);
        modelBuilder.Entity<Product>().Property(x => x.UnitPrice).HasPrecision(18, 2);
        modelBuilder.Entity<VenueTable>().Property(x => x.PositionX).HasPrecision(10, 2);
        modelBuilder.Entity<VenueTable>().Property(x => x.PositionY).HasPrecision(10, 2);

        base.OnModelCreating(modelBuilder);
    }
}
