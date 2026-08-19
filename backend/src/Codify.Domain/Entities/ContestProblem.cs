namespace Codify.Domain.Entities;

public class ContestProblem
{
    public Guid ContestId { get; set; }
    public Contest Contest { get; set; } = null!;

    public Guid ProblemId { get; set; }
    public Problem Problem { get; set; } = null!;

    public int Order { get; set; } = 1;
    public int Points { get; set; } = 100;
}
