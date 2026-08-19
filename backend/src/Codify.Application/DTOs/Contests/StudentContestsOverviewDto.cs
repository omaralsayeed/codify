namespace Codify.Application.DTOs.Contests;

public class StudentContestsOverviewDto
{
    public bool HasActiveContestNotification { get; set; }
    public int ActiveContestsCount { get; set; }
    public List<ContestDto> PendingInvitations { get; set; } = [];
    public List<ContestDto> LiveContests { get; set; } = [];
    public List<ContestDto> UpcomingContests { get; set; } = [];
    public List<StudentPastContestDto> PastContests { get; set; } = [];
}

public class StudentPastContestDto
{
    public Guid ContestId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int TotalProblems { get; set; }
    public int ProblemsSolved { get; set; }
    public int Score { get; set; }
    public int Rank { get; set; }
    public double Accuracy { get; set; }
    public DateTime? FinishedAt { get; set; }
    public List<ContestProblemDetailDto> Problems { get; set; } = [];
}
