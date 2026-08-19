using Codify.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codify.Infrastructure.Persistence.Configurations;

public class InstructorStudentConfiguration : IEntityTypeConfiguration<InstructorStudent>
{
    public void Configure(EntityTypeBuilder<InstructorStudent> builder)
    {
        builder.HasKey(x => new { x.InstructorId, x.StudentId });

        builder.HasOne(x => x.Instructor)
            .WithMany()
            .HasForeignKey(x => x.InstructorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.EnrolledAt).IsRequired();
    }
}
