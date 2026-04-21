using Codify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codify.Infrastructure.Persistence.Configurations;

public class ConceptTagConfiguration : IEntityTypeConfiguration<ConceptTag>
{
    public void Configure(EntityTypeBuilder<ConceptTag> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(t => t.Name).IsUnique();
        builder.Property(t => t.Description).HasColumnType("nvarchar(max)");
    }
}
