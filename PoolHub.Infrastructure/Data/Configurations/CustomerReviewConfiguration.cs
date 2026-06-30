using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PoolHub.Core.Entities;

namespace PoolHub.Infrastructure.Data.Configurations;

public class CustomerReviewConfiguration : IEntityTypeConfiguration<CustomerReview>
{
    public void Configure(EntityTypeBuilder<CustomerReview> builder)
    {
        builder.ToTable("customer_reviews", table =>
        {
            table.HasCheckConstraint("CK_customer_reviews_rating", "[rating] >= 1 AND [rating] <= 5");
            table.HasCheckConstraint("CK_customer_reviews_status", "[status] IN (1, 2, 3, 4)");
        });
        builder.HasKey(x => x.CustomerReviewId);
        builder.HasIndex(x => x.PublicId).IsUnique();
        builder.HasIndex(x => new { x.Status, x.IsFeatured, x.DisplayOrder });
        builder.HasIndex(x => new { x.CustomerId, x.BookingId })
            .IsUnique()
            .HasFilter("[customer_id] IS NOT NULL AND [booking_id] IS NOT NULL AND [status] IN (1, 2)");
        builder.HasIndex(x => new { x.CustomerId, x.SessionId })
            .IsUnique()
            .HasFilter("[customer_id] IS NOT NULL AND [session_id] IS NOT NULL AND [status] IN (1, 2)");
        builder.HasIndex(x => new { x.CustomerId, x.InvoiceId })
            .IsUnique()
            .HasFilter("[customer_id] IS NOT NULL AND [invoice_id] IS NOT NULL AND [status] IN (1, 2)");

        builder.Property(x => x.Content).HasMaxLength(1000);
        builder.Property(x => x.DisplayName).HasMaxLength(150);
        builder.Property(x => x.AvatarUrl).HasMaxLength(500);
        builder.Property(x => x.CheckInImageUrl).HasMaxLength(500);
        builder.Property(x => x.Source).HasMaxLength(40);
        builder.Property(x => x.RejectedReason).HasMaxLength(500);
        builder.Property(x => x.Note).HasMaxLength(500);

        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Booking>().WithMany().HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Session>().WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Invoice>().WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.ApprovedByUserId).OnDelete(DeleteBehavior.NoAction);
    }
}
