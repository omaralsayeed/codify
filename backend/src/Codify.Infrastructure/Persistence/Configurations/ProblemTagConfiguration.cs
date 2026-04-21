using Codify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codify.Infrastructure.Persistence.Configurations;

public class ProblemTagConfiguration : IEntityTypeConfiguration<ProblemTag>
{
    public void Configure(EntityTypeBuilder<ProblemTag> builder)
    {
        builder.HasKey(pt => new { pt.ProblemId, pt.ConceptTagId });

        builder.HasOne(pt => pt.Problem)
            .WithMany(p => p.ProblemTags)
            .HasForeignKey(pt => pt.ProblemId);

        builder.HasOne(pt => pt.ConceptTag)
            .WithMany(t => t.ProblemTags)
            .HasForeignKey(pt => pt.ConceptTagId);
    }
}
