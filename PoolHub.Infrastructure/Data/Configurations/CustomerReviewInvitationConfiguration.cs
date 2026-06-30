using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PoolHub.Core.Entities;

namespace PoolHub.Infrastructure.Data.Configurations;

public class CustomerReviewInvitationConfiguration : IEntityTypeConfiguration<CustomerReviewInvitation>
{
    public void Configure(EntityTypeBuilder<CustomerReviewInvitation> builder)
    {
        builder.ToTable("customer_review_invitations", table =>
        {
            table.HasCheckConstraint("CK_customer_review_invitations_status", "[status] IN (1, 2, 3, 4)");
        });
        builder.HasKey(x => x.CustomerReviewInvitationId);
        builder.HasIndex(x => x.PublicId).IsUnique();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => new { x.InvoiceId, x.Status });
        builder.Property(x => x.TokenHash).HasMaxLength(128);

        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Session>().WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Invoice>().WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
    }
}
