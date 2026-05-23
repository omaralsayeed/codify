using Codify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codify.Infrastructure.Persistence.Configurations;

public class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Code).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(s => s.Language).HasConversion<string>();
        builder.Property(s => s.Status).HasConversion<string>();
        builder.Property(s => s.SubmittedAt).IsRequired();
        builder.Property(s => s.PassedTestCases).HasDefaultValue(0);
        builder.Property(s => s.TotalTestCases).HasDefaultValue(0);
        builder.Property(s => s.Score).HasColumnType("decimal(5,2)");
        builder.Property(s => s.UpdatedAt).IsRequired();
        builder.Property(s => s.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasQueryFilter(s => !s.IsDeleted);

        builder.HasOne(s => s.User)
            .WithMany(u => u.Submissions)
            .HasForeignKey(s => s.UserId);

        builder.HasOne(s => s.Problem)
            .WithMany(p => p.Submissions)
            .HasForeignKey(s => s.ProblemId);

        builder.HasOne(s => s.Result)
            .WithOne(r => r.Submission)
            .HasForeignKey<SubmissionResult>(r => r.SubmissionId);
    }
}
