using Codify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codify.Infrastructure.Persistence.Configurations;

public class TestCaseResultConfiguration : IEntityTypeConfiguration<TestCaseResult>
{
    public void Configure(EntityTypeBuilder<TestCaseResult> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Verdict).HasConversion<string>();
        builder.Property(r => r.ActualOutput).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(r => r.Stderr).HasColumnType("nvarchar(max)");
        builder.Property(r => r.CreatedAt).IsRequired();

        builder.HasIndex(r => new { r.SubmissionId, r.TestCaseId }).IsUnique();

        builder.HasOne(r => r.Submission)
            .WithMany(s => s.TestCaseResults)
            .HasForeignKey(r => r.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.TestCase)
            .WithMany()
            .HasForeignKey(r => r.TestCaseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
