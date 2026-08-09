using Codify.Domain.Enums;

namespace Codify.Domain.Entities;

/// <summary>
/// The outcome of running a single test case against a single submission.
/// One <see cref="Submission"/> has one <see cref="TestCaseResult"/> per <see cref="TestCase"/>
/// that was executed (public and hidden alike).
/// </summary>
public class TestCaseResult
{
    public Guid Id { get; private set; }
    public Guid SubmissionId { get; private set; }
    public Guid TestCaseId { get; private set; }

    /// <summary>Per-test verdict (Accepted, WrongAnswer, TimeLimitExceeded, RuntimeError, CompileError, MemoryLimitExceeded).</summary>
    public SubmissionStatus Verdict { get; private set; }

    public string ActualOutput { get; private set; } = string.Empty;
    public string? Stderr { get; private set; }
    public int ExecutionTimeMs { get; private set; }
    public int MemoryUsedKb { get; private set; }

    /// <summary>Denormalized copy of TestCase.IsSample at execution time — lets us gate what students can see.</summary>
    public bool IsSample { get; private set; }
    public int OrderIndex { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public Submission Submission { get; private set; } = null!;
    public TestCase TestCase { get; private set; } = null!;

    private TestCaseResult() { }

    public static TestCaseResult Create(
        Guid submissionId,
        Guid testCaseId,
        SubmissionStatus verdict,
        string actualOutput,
        string? stderr,
        int executionTimeMs,
        int memoryUsedKb,
        bool isSample,
        int orderIndex)
    {
        return new TestCaseResult
        {
            Id = Guid.NewGuid(),
            SubmissionId = submissionId,
            TestCaseId = testCaseId,
            Verdict = verdict,
            ActualOutput = actualOutput,
            Stderr = stderr,
            ExecutionTimeMs = executionTimeMs,
            MemoryUsedKb = memoryUsedKb,
            IsSample = isSample,
            OrderIndex = orderIndex,
            CreatedAt = DateTime.UtcNow
        };
    }
}
