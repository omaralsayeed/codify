using Codify.Application.DTOs.AI;
using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Domain.Enums;
using Codify.Infrastructure.AI;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Codify.Tests.Infrastructure;

public class TutorAgentToolsTests
{
    private readonly ISubmissionRepository _submissionRepo = Substitute.For<ISubmissionRepository>();
    private readonly IHintLogRepository _hintLogRepo = Substitute.For<IHintLogRepository>();
    private readonly IKnowledgeBaseSearchService _knowledgeBase = Substitute.For<IKnowledgeBaseSearchService>();
    private readonly TutorAgentTools _sut;

    public TutorAgentToolsTests()
    {
        _sut = new TutorAgentTools(
            _submissionRepo,
            _hintLogRepo,
            _knowledgeBase,
            NullLogger<TutorAgentTools>.Instance);
    }

    [Fact]
    public async Task GetAttemptHistoryAsync_ShouldAggregateSubmissionsAndHints()
    {
        var studentId = Guid.NewGuid();
        var problemId = Guid.NewGuid();

        var submissions = new List<Submission>
        {
            Submission.Create(problemId, studentId, "code1", SubmissionLanguage.Python),
            Submission.Create(problemId, studentId, "code2", SubmissionLanguage.Python)
        };
        submissions[0].MarkAsAccepted(10, 100, 5, 5);
        submissions[1].MarkAsFailed(SubmissionStatus.WrongAnswer, 2, 5);

        var hints = new List<HintLog>
        {
            HintLog.Create(studentId, problemId, 1, "q1", "hint1"),
            HintLog.Create(studentId, problemId, 2, "q2", "hint2")
        };

        _submissionRepo.GetByProblemAndUserAsync(problemId, studentId).Returns(submissions);
        _hintLogRepo.GetByUserAndProblemAsync(studentId, problemId).Returns(hints);

        var result = await _sut.GetAttemptHistoryAsync(studentId, problemId);

        Assert.Equal(2, result.AttemptCount);
        Assert.Contains("Accepted", result.SubmissionStatuses);
        Assert.Contains("WrongAnswer", result.SubmissionStatuses);
        Assert.Equal(new[] { 1, 2 }, result.PreviousHintLevels);
        Assert.Equal(2, result.Timestamps.Count);
    }

    [Fact]
    public async Task SearchKnowledgeBaseAsync_ShouldDelegateToSearchService()
    {
        var query = "dynamic programming memoization";
        var tag = "dynamic-programming";
        var expected = new List<KnowledgeBaseResult>
        {
            new() { Content = "Store subproblem results.", ConceptTag = "dynamic-programming", Relevance = 0.95 }
        };
        _knowledgeBase.SearchAsync(query, tag).Returns(expected);

        var result = await _sut.SearchKnowledgeBaseAsync(query, tag);

        Assert.Single(result);
        Assert.Equal("Store subproblem results.", result[0].Content);
    }

    [Theory]
    [InlineData("def f(n):\n    return f(n-1)", "python", true, false)]
    [InlineData("public int Fib(int n) => n <= 1 ? n : Fib(n-1) + Fib(n-2);", "csharp", true, true)]
    [InlineData("for i in range(10): print(i)", "python", false, false)]
    public async Task CheckPartialCodeAsync_ShouldDetectStructure(
        string code, string language, bool expectsRecursion, bool expectsBaseCase)
    {
        var result = await _sut.CheckPartialCodeAsync(code, language);

        Assert.NotNull(result);
        Assert.True(result.SyntaxValid);
        Assert.Contains(result.Observations, o => o.Contains("No loop detected") == !code.Contains("for "));

        if (expectsRecursion)
            Assert.Contains(result.Observations, o => o.Contains("Recursive call present"));

        if (expectsRecursion && !expectsBaseCase)
            Assert.Contains(result.Observations, o => o.Contains("no obvious base case"));
    }

    [Fact]
    public async Task GetPreviousHintsAsync_ShouldReturnOrderedHintText()
    {
        var studentId = Guid.NewGuid();
        var problemId = Guid.NewGuid();
        var hints = new List<HintLog>
        {
            HintLog.Create(studentId, problemId, 1, "first hint", "q"),
            HintLog.Create(studentId, problemId, 2, "second hint", "q")
        };
        _hintLogRepo.GetByUserAndProblemAsync(studentId, problemId).Returns(hints);

        var result = await _sut.GetPreviousHintsAsync(studentId, problemId);

        Assert.Equal(2, result.Count);
        Assert.Equal("first hint", result[0].HintText);
        Assert.Equal("second hint", result[1].HintText);
    }
}
