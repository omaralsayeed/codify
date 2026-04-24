using Codify.Domain.Enums;

namespace Codify.Domain.Entities;

public class TestCase
{
    public Guid Id { get; private set; }
    public Guid ProblemId { get; private set; }
    public string InputData { get; private set; } = string.Empty;
    public string ExpectedOutput { get; private set; } = string.Empty;
    public bool IsSample { get; private set; }
    public TestCaseVisibility VisibilityMode { get; private set; }

    // ER diagram additions
    public int OrderIndex { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation
    public Problem Problem { get; private set; } = null!;

    private TestCase() { }

    public static TestCase Create(
        Guid problemId,
        string inputData,
        string expectedOutput,
        bool isSample,
        TestCaseVisibility visibilityMode,
        int orderIndex = 0)
    {
        return new TestCase
        {
            Id = Guid.NewGuid(),
            ProblemId = problemId,
            InputData = inputData,
            ExpectedOutput = expectedOutput,
            IsSample = isSample,
            VisibilityMode = visibilityMode,
            OrderIndex = orderIndex,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }
}
