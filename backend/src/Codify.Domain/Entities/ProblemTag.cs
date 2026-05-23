namespace Codify.Domain.Entities;

public class ProblemTag
{
    public Guid ProblemId { get; private set; }
    public Guid ConceptTagId { get; private set; }

    // Navigation
    public Problem Problem { get; private set; } = null!;
    public ConceptTag ConceptTag { get; private set; } = null!;

    private ProblemTag() { }

    public static ProblemTag Create(Guid problemId, Guid conceptTagId)
    {
        return new ProblemTag
        {
            ProblemId = problemId,
            ConceptTagId = conceptTagId
        };
    }
}
