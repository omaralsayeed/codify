using Codify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codify.Infrastructure.Persistence.Configurations;

public class PerformanceProfileConfiguration : IEntityTypeConfiguration<PerformanceProfile>
{
    public void Configure(EntityTypeBuilder<PerformanceProfile> builder)
    {
        builder.HasKey(p => p.UserId);
        builder.Property(p => p.WeakTopicsJson).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(p => p.StrongTopicsJson).IsRequired().HasColumnType("nvarchar(max)");
    }
}
