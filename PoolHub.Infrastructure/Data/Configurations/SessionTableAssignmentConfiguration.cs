using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PoolHub.Core.Entities;

namespace PoolHub.Infrastructure.Data.Configurations;

public class SessionTableAssignmentConfiguration : IEntityTypeConfiguration<SessionTableAssignment>
{
    public void Configure(EntityTypeBuilder<SessionTableAssignment> builder)
    {
        builder.ToTable("session_table_assignments");
        builder.HasKey(x => x.SessionTableAssignmentId);
        builder.Property(x => x.HourlyRateSnapshot).HasPrecision(19, 4);
        builder.Property(x => x.Amount).HasPrecision(19, 4);
        builder.HasOne<Session>().WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VenueTable>().WithMany().HasForeignKey(x => x.TableId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PricingPlanRule>().WithMany().HasForeignKey(x => x.PricingPlanRuleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.AssignedByUserId).OnDelete(DeleteBehavior.NoAction);
    }
}
