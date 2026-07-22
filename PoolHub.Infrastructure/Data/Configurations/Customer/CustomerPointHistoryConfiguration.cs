using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PoolHub.Core.Entities;

namespace PoolHub.Infrastructure.Data.Configurations;

public class CustomerPointHistoryConfiguration : IEntityTypeConfiguration<CustomerPointHistory>
{
    public void Configure(EntityTypeBuilder<CustomerPointHistory> builder)
    {
        builder.ToTable("customer_point_histories");
        builder.HasKey(x => x.CustomerPointHistoryId);
        builder.HasIndex(x => x.CustomerId);
    }
}
