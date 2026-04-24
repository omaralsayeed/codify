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
        builder.Property(p => p.Slug).IsRequired().HasMaxLength(350);
        builder.HasIndex(p => p.Slug).IsUnique();
        builder.Property(p => p.Statement).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(p => p.Difficulty).HasConversion<string>();
        builder.Property(p => p.LanguageSupportJson).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(p => p.Constraints).HasColumnType("nvarchar(max)");
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();
        builder.Property(p => p.IsActive).IsRequired();
        builder.Property(p => p.IsPublic).IsRequired().HasDefaultValue(true);
        builder.Property(p => p.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(p => p.TimeLimitMs).IsRequired().HasDefaultValue(2000);
        builder.Property(p => p.MemoryLimitMb).IsRequired().HasDefaultValue(256);
        builder.Property(p => p.AcceptedSubmissionsCount).HasDefaultValue(0);
        builder.Property(p => p.TotalSubmissionsCount).HasDefaultValue(0);

        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasOne(p => p.Author)
            .WithMany(u => u.AuthoredProblems)
            .HasForeignKey(p => p.AuthorId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
