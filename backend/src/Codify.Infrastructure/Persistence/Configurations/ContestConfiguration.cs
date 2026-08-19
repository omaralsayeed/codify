using Codify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codify.Infrastructure.Persistence.Configurations;

public class ContestConfiguration : IEntityTypeConfiguration<Contest>
{
    public void Configure(EntityTypeBuilder<Contest> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Title).IsRequired().HasMaxLength(300);
        builder.Property(c => c.Description).HasColumnType("nvarchar(max)");
        builder.Property(c => c.Status).HasConversion<int>();
        builder.Property(c => c.StartAt).IsRequired();
        builder.Property(c => c.EndAt).IsRequired();
        builder.Property(c => c.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(c => c.CreatedAt).IsRequired();

        builder.HasQueryFilter(c => !c.IsDeleted);

        builder.HasOne(c => c.CreatedByInstructor)
            .WithMany()
            .HasForeignKey(c => c.CreatedByInstructorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.ContestProblems)
            .WithOne(cp => cp.Contest)
            .HasForeignKey(cp => cp.ContestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.ContestParticipants)
            .WithOne(cp => cp.Contest)
            .HasForeignKey(cp => cp.ContestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ContestProblemConfiguration : IEntityTypeConfiguration<ContestProblem>
{
    public void Configure(EntityTypeBuilder<ContestProblem> builder)
    {
        builder.HasKey(cp => new { cp.ContestId, cp.ProblemId });

        builder.HasOne(cp => cp.Problem)
            .WithMany()
            .HasForeignKey(cp => cp.ProblemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ContestParticipantConfiguration : IEntityTypeConfiguration<ContestParticipant>
{
    public void Configure(EntityTypeBuilder<ContestParticipant> builder)
    {
        builder.HasKey(cp => new { cp.ContestId, cp.StudentId });

        builder.Property(cp => cp.InvitationStatus).HasConversion<int>().IsRequired();
        builder.Property(cp => cp.InvitedEmail).HasMaxLength(320);
        builder.Property(cp => cp.RespondedAt);

        builder.HasOne(cp => cp.Student)
            .WithMany()
            .HasForeignKey(cp => cp.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
