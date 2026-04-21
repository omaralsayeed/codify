using Codify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codify.Infrastructure.Persistence.Configurations;

public class FeedbackRecordConfiguration : IEntityTypeConfiguration<FeedbackRecord>
{
    public void Configure(EntityTypeBuilder<FeedbackRecord> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.FeedbackType).HasConversion<string>();
        builder.Property(f => f.Message).IsRequired().HasColumnType("nvarchar(max)");

        builder.HasOne(f => f.Submission)
            .WithMany(s => s.FeedbackRecords)
            .HasForeignKey(f => f.SubmissionId);
    }
}
