using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PoolHub.Core.Entities;

namespace PoolHub.Infrastructure.Data.Configurations;

public class PricingPlanRuleConfiguration : IEntityTypeConfiguration<PricingPlanRule>
{
    public void Configure(EntityTypeBuilder<PricingPlanRule> builder)
    {
        builder.ToTable("pricing_plan_rules");
        builder.HasKey(x => x.PricingPlanRuleId);
        builder.HasIndex(x => new { x.PricingPlanId, x.TableTypeId, x.DayType, x.StartTime }).IsUnique();
        builder.Property(x => x.HourlyRate).HasPrecision(19, 4);
        builder.HasOne<PricingPlan>().WithMany().HasForeignKey(x => x.PricingPlanId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TableType>().WithMany().HasForeignKey(x => x.TableTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}
