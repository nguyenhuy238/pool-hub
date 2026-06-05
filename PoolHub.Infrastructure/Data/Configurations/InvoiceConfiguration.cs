using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PoolHub.Core.Entities;

namespace PoolHub.Infrastructure.Data.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");
        builder.HasKey(x => x.InvoiceId);
        builder.HasIndex(x => x.PublicId).IsUnique();
        builder.HasIndex(x => x.InvoiceCode).IsUnique();
        builder.HasIndex(x => x.SessionId).IsUnique();
        builder.Property(x => x.TimeSubtotalAmount).HasPrecision(19, 4);
        builder.Property(x => x.ProductSubtotalAmount).HasPrecision(19, 4);
        builder.Property(x => x.SubtotalAmount).HasPrecision(19, 4);
        builder.Property(x => x.DiscountAmount).HasPrecision(19, 4);
        builder.Property(x => x.TaxAmount).HasPrecision(19, 4);
        builder.Property(x => x.GrandTotalAmount).HasPrecision(19, 4);
        builder.Property(x => x.PaidAmount).HasPrecision(19, 4);
        builder.HasOne<Session>().WithOne().HasForeignKey<Invoice>(x => x.SessionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.IssuedByUserId).OnDelete(DeleteBehavior.NoAction);
    }
}
