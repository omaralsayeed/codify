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

    // Navigation
    public Problem Problem { get; private set; } = null!;

    private TestCase() { }

    public static TestCase Create(
        Guid problemId,
        string inputData,
        string expectedOutput,
        bool isSample,
        TestCaseVisibility visibilityMode)
    {
        return new TestCase
        {
            Id = Guid.NewGuid(),
            ProblemId = problemId,
            InputData = inputData,
            ExpectedOutput = expectedOutput,
            IsSample = isSample,
            VisibilityMode = visibilityMode
        };
    }
}
