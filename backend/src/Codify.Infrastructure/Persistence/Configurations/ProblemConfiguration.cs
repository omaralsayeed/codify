using Codify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codify.Infrastructure.Persistence.Configurations;

public class ProblemConfiguration : IEntityTypeConfiguration<Problem>
{
    public void Configure(EntityTypeBuilder<Problem> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Title).IsRequired().HasMaxLength(300);
        builder.Property(p => p.Statement).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(p => p.Difficulty).HasConversion<string>();
        builder.Property(p => p.LanguageSupportJson).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(p => p.Constraints).HasColumnType("nvarchar(max)");
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.IsActive).IsRequired();
    }
}
