using Codify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codify.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.FullName).IsRequired().HasMaxLength(200);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(320);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.Role).HasConversion<string>();
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.UpdatedAt).IsRequired();
        builder.Property(u => u.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(u => u.Rating).HasColumnType("decimal(10,2)").HasDefaultValue(0);
        builder.Property(u => u.SolvedProblems).HasDefaultValue(0);
        builder.Property(u => u.Username).HasMaxLength(100);
        builder.Property(u => u.Bio).HasColumnType("nvarchar(max)");
        builder.Property(u => u.AvatarUrl).HasMaxLength(500);

        builder.HasQueryFilter(u => !u.IsDeleted);

        builder.HasOne(u => u.PerformanceProfile)
            .WithOne(p => p.User)
            .HasForeignKey<PerformanceProfile>(p => p.UserId);
    }
}
