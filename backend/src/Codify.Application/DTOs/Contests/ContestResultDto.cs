namespace Codify.Application.DTOs.Contests;

public class ContestResultDto
{
    public Guid ContestId { get; set; }
    public string ContestTitle { get; set; } = string.Empty;
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int Rank { get; set; }
    public int Score { get; set; }
    public int ProblemsSolved { get; set; }
    public int TotalProblems { get; set; }
    public double Accuracy { get; set; }
    public DateTime? FinishedAt { get; set; }
}
