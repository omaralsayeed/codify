using Codify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codify.Infrastructure.Persistence.Configurations;

public class HintLogConfiguration : IEntityTypeConfiguration<HintLog>
{
    public void Configure(EntityTypeBuilder<HintLog> builder)
    {
        builder.HasKey(h => h.Id);
        builder.Property(h => h.ResponseText).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(h => h.RequestText).HasColumnType("nvarchar(max)");

        builder.HasOne(h => h.User)
            .WithMany(u => u.HintLogs)
            .HasForeignKey(h => h.UserId);
    }
}
