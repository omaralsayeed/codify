using Codify.Domain.Entities;
using Codify.Domain.Enums;

namespace Codify.Tests.Domain;

public class SubmissionEntityTests
{
    [Fact]
    public void Create_ShouldSetPendingStatus()
    {
        var submission = Submission.Create(Guid.NewGuid(), Guid.NewGuid(), "print('hello')", SubmissionLanguage.Python);

        Assert.Equal(SubmissionStatus.Pending, submission.Status);
        Assert.NotEqual(Guid.Empty, submission.Id);
        Assert.Equal(0, submission.PassedTestCases);
        Assert.Equal(0, submission.TotalTestCases);
        Assert.Null(submission.Score);
        Assert.False(submission.IsDeleted);
    }

    [Fact]
    public void MarkAsRunning_ShouldTransitionToRunning()
    {
        var submission = Submission.Create(Guid.NewGuid(), Guid.NewGuid(), "code", SubmissionLanguage.CSharp);

        submission.MarkAsRunning();

        Assert.Equal(SubmissionStatus.Running, submission.Status);
    }

    [Fact]
    public void MarkAsAccepted_ShouldSetAllFields()
    {
        var submission = Submission.Create(Guid.NewGuid(), Guid.NewGuid(), "code", SubmissionLanguage.Python);

        submission.MarkAsAccepted(executionTimeMs: 42, memoryUsedKb: 8192, passedTestCases: 10, totalTestCases: 10);

        Assert.Equal(SubmissionStatus.Accepted, submission.Status);
        Assert.Equal(42, submission.ExecutionTimeMs);
        Assert.Equal(8192, submission.MemoryUsedKb);
        Assert.Equal(10, submission.PassedTestCases);
        Assert.Equal(10, submission.TotalTestCases);
        Assert.Equal(100m, submission.Score);
    }

    [Fact]
    public void MarkAsFailed_ShouldSetPartialScore()
    {
        var submission = Submission.Create(Guid.NewGuid(), Guid.NewGuid(), "code", SubmissionLanguage.Python);

        submission.MarkAsFailed(SubmissionStatus.WrongAnswer, passedTestCases: 3, totalTestCases: 10);

        Assert.Equal(SubmissionStatus.WrongAnswer, submission.Status);
        Assert.Equal(3, submission.PassedTestCases);
        Assert.Equal(10, submission.TotalTestCases);
        Assert.Equal(30m, submission.Score);
    }

    [Fact]
    public void MarkAsFailed_WithZeroTotal_ShouldSetScoreToZero()
    {
        var submission = Submission.Create(Guid.NewGuid(), Guid.NewGuid(), "code", SubmissionLanguage.Python);

        submission.MarkAsFailed(SubmissionStatus.CompileError, passedTestCases: 0, totalTestCases: 0);

        Assert.Equal(0m, submission.Score);
    }

    [Fact]
    public void SoftDelete_ShouldMarkAsDeleted()
    {
        var submission = Submission.Create(Guid.NewGuid(), Guid.NewGuid(), "code", SubmissionLanguage.Python);

        submission.SoftDelete();

        Assert.True(submission.IsDeleted);
    }
}
