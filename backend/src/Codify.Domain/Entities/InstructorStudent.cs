namespace Codify.Domain.Entities;

public class InstructorStudent
{
    public Guid InstructorId { get; set; }
    public User Instructor { get; set; } = null!;

    public Guid StudentId { get; set; }
    public User Student { get; set; } = null!;

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
}
