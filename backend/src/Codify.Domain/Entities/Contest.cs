using Codify.Domain.Enums;

namespace Codify.Domain.Entities;

public class Contest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public Guid CreatedByInstructorId { get; set; }
    public User CreatedByInstructor { get; set; } = null!;

    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public ContestStatus Status { get; set; } = ContestStatus.Upcoming;

    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ContestProblem> ContestProblems { get; set; } = new List<ContestProblem>();
    public ICollection<ContestParticipant> ContestParticipants { get; set; } = new List<ContestParticipant>();

    public ContestStatus CalculateCurrentStatus()
    {
        var now = DateTime.UtcNow;
        if (now < StartAt) return ContestStatus.Upcoming;
        if (now > EndAt) return ContestStatus.Ended;
        return ContestStatus.Live;
    }
}
