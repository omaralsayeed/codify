using Codify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codify.Infrastructure.Persistence.Configurations;

public class TestCaseConfiguration : IEntityTypeConfiguration<TestCase>
{
    public void Configure(EntityTypeBuilder<TestCase> builder)
    {
        builder.HasKey(tc => tc.Id);
        builder.Property(tc => tc.InputData).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(tc => tc.ExpectedOutput).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(tc => tc.VisibilityMode).HasConversion<string>();

        builder.HasOne(tc => tc.Problem)
            .WithMany(p => p.TestCases)
            .HasForeignKey(tc => tc.ProblemId);
    }
}
