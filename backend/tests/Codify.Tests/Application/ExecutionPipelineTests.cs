using Codify.Application.DTOs.Submissions;
using Codify.Application.Interfaces;
using Codify.Application.Services;
using Codify.Domain.Entities;
using Codify.Domain.Enums;
using Codify.Domain.Exceptions;
using NSubstitute;

namespace Codify.Tests.Application;

/// <summary>
/// Tests for the submission pipeline entry point. Evaluation now happens off the
/// request thread: SubmissionService persists a Pending submission and enqueues it
/// for the background JudgeEvaluationService. These tests verify that handoff.
/// </summary>
public class ExecutionPipelineTests
{
    private readonly ISubmissionRepository _submissionRepo = Substitute.For<ISubmissionRepository>();
    private readonly IProblemRepository _problemRepo = Substitute.For<IProblemRepository>();
    private readonly IFeedbackRepository _feedbackRepo = Substitute.For<IFeedbackRepository>();
    private readonly ISubmissionEvaluationQueue _evaluationQueue = Substitute.For<ISubmissionEvaluationQueue>();
    private readonly SubmissionService _sut;

    public ExecutionPipelineTests()
    {
        _sut = new SubmissionService(
            _submissionRepo, _problemRepo, _feedbackRepo, _evaluationQueue);
    }

    private static Problem MakeProblem(params (string Input, string Expected)[] cases)
    {
        var p = Problem.Create("Test", "...", Difficulty.Easy, "", "[]");
        int i = 0;
        foreach (var (input, expected) in cases)
            p.TestCases.Add(TestCase.Create(p.Id, input, expected, false, TestCaseVisibility.Hidden, i++));
        return p;
    }

    [Fact]
    public async Task Pipeline_ShouldReturnPending_AndQueueSubmission_ForBackgroundEvaluation()
    {
        var userId = Guid.NewGuid();
        var problem = MakeProblem(("1", "1"));
        _problemRepo.GetByIdWithTestCasesAsync(problem.Id).Returns(problem);
        _submissionRepo.GetByIdWithDetailsAsync(Arg.Any<Guid>())
            .Returns(_ => Submission.Create(problem.Id, userId, "code", SubmissionLanguage.Python));

        var result = await _sut.CreateAsync(new CreateSubmissionRequest
        {
            ProblemId = problem.Id, Code = "code", Language = SubmissionLanguage.Python
        }, userId);

        // The submission is accepted as Pending and handed to the background queue.
        Assert.Equal("Pending", result.Status);
        await _submissionRepo.Received(1).AddAsync(Arg.Any<Submission>());
        _evaluationQueue.Received(1).QueueSubmission(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Pipeline_ShouldThrowNotFound_WhenProblemMissing()
    {
        _problemRepo.GetByIdWithTestCasesAsync(Arg.Any<Guid>()).Returns((Problem?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.CreateAsync(new CreateSubmissionRequest
        {
            ProblemId = Guid.NewGuid(), Code = "code", Language = SubmissionLanguage.Python
        }, Guid.NewGuid()));

        // Nothing should be queued when the problem does not exist.
        _evaluationQueue.DidNotReceive().QueueSubmission(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Pipeline_ShouldPersistBeforeQueuing()
    {
        var userId = Guid.NewGuid();
        var problem = MakeProblem(("1", "1"));
        _problemRepo.GetByIdWithTestCasesAsync(problem.Id).Returns(problem);
        _submissionRepo.GetByIdWithDetailsAsync(Arg.Any<Guid>())
            .Returns(_ => Submission.Create(problem.Id, userId, "code", SubmissionLanguage.Python));

        await _sut.CreateAsync(new CreateSubmissionRequest
        {
            ProblemId = problem.Id, Code = "code", Language = SubmissionLanguage.Python
        }, userId);

        // Persist (AddAsync + SaveChangesAsync) must both occur before the queue handoff.
        await _submissionRepo.Received(1).AddAsync(Arg.Any<Submission>());
        await _submissionRepo.Received(1).SaveChangesAsync();
        _evaluationQueue.Received(1).QueueSubmission(Arg.Any<Guid>());
    }
}
