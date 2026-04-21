using Codify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codify.Infrastructure.Persistence.Configurations;

public class SubmissionResultConfiguration : IEntityTypeConfiguration<SubmissionResult>
{
    public void Configure(EntityTypeBuilder<SubmissionResult> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.ErrorMessage).HasColumnType("nvarchar(max)");
        builder.Property(r => r.OutputSummary).HasColumnType("nvarchar(max)");
    }
}
