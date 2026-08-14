using Codify.Application.DTOs.Execution;
using Codify.Application.DTOs.Submissions;
using Codify.Application.Interfaces;
using Codify.Application.Services;
using Codify.Domain.Entities;
using Codify.Domain.Enums;
using Codify.Domain.Exceptions;
using NSubstitute;

namespace Codify.Tests.Application;

public class SubmissionServiceTests
{
    private readonly ISubmissionRepository _submissionRepo = Substitute.For<ISubmissionRepository>();
    private readonly IProblemRepository _problemRepo = Substitute.For<IProblemRepository>();
    private readonly IFeedbackRepository _feedbackRepo = Substitute.For<IFeedbackRepository>();
    private readonly ISubmissionEvaluationQueue _evaluationQueue = Substitute.For<ISubmissionEvaluationQueue>();
    private readonly SubmissionService _sut;

    public SubmissionServiceTests()
    {
        _sut = new SubmissionService(
            _submissionRepo,
            _problemRepo,
            _feedbackRepo,
            _evaluationQueue);
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistPendingSubmission_AndQueueForEvaluation()
    {
        var problemId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var problem = Problem.Create("Two Sum", "...", Difficulty.Easy, "", "[]");
        problem.TestCases.Add(TestCase.Create(problem.Id, "input1", "output1", true, TestCaseVisibility.Public, 0));

        _problemRepo.GetByIdWithTestCasesAsync(problemId).Returns(problem);
        _submissionRepo.GetByIdWithDetailsAsync(Arg.Any<Guid>())
            .Returns(callInfo => Submission.Create(problemId, userId, "code", SubmissionLanguage.Python));

        var request = new CreateSubmissionRequest
        {
            ProblemId = problemId,
            Code = "print('output1')",
            Language = SubmissionLanguage.Python
        };

        var result = await _sut.CreateAsync(request, userId);

        // Submission is persisted and handed off to the background evaluation queue.
        await _submissionRepo.Received(1).AddAsync(Arg.Any<Submission>());
        await _submissionRepo.Received(1).SaveChangesAsync();
        _evaluationQueue.Received(1).QueueSubmission(Arg.Any<Guid>());
        Assert.Equal("Pending", result.Status);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowNotFoundException_WhenProblemDoesNotExist()
    {
        var problemId = Guid.NewGuid();
        _problemRepo.GetByIdWithTestCasesAsync(problemId).Returns((Problem?)null);

        var request = new CreateSubmissionRequest
        {
            ProblemId = problemId,
            Code = "code",
            Language = SubmissionLanguage.Python
        };

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.CreateAsync(request, Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnSubmission_WhenOwnerRequests()
    {
        var userId = Guid.NewGuid();
        var submission = Submission.Create(Guid.NewGuid(), userId, "code", SubmissionLanguage.Python);

        _submissionRepo.GetByIdWithDetailsAsync(submission.Id).Returns(submission);

        var result = await _sut.GetByIdAsync(submission.Id, userId, isInstructor: false);

        Assert.Equal(submission.Id, result.SubmissionId);
        Assert.Equal(userId, result.UserId);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowForbidden_WhenStudentAccessesOtherSubmission()
    {
        var ownerId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var submission = Submission.Create(Guid.NewGuid(), ownerId, "code", SubmissionLanguage.Python);

        _submissionRepo.GetByIdWithDetailsAsync(submission.Id).Returns(submission);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _sut.GetByIdAsync(submission.Id, requesterId, isInstructor: false));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldAllowInstructor_ToAccessAnySubmission()
    {
        var ownerId = Guid.NewGuid();
        var instructorId = Guid.NewGuid();
        var submission = Submission.Create(Guid.NewGuid(), ownerId, "code", SubmissionLanguage.Python);

        _submissionRepo.GetByIdWithDetailsAsync(submission.Id).Returns(submission);

        var result = await _sut.GetByIdAsync(submission.Id, instructorId, isInstructor: true);

        Assert.Equal(submission.Id, result.SubmissionId);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowNotFound_WhenSubmissionDoesNotExist()
    {
        var id = Guid.NewGuid();
        _submissionRepo.GetByIdWithDetailsAsync(id).Returns((Submission?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.GetByIdAsync(id, Guid.NewGuid(), isInstructor: false));
    }

    [Fact]
    public async Task GetByProblemAsync_ShouldFilterByUserId_ForStudents()
    {
        var problemId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _submissionRepo.GetByProblemAndUserAsync(problemId, userId).Returns([]);

        await _sut.GetByProblemAsync(problemId, userId, isInstructor: false);

        await _submissionRepo.Received(1).GetByProblemAndUserAsync(problemId, userId);
    }

    [Fact]
    public async Task GetByProblemAsync_ShouldNotFilterByUserId_ForInstructors()
    {
        var problemId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _submissionRepo.GetByProblemAndUserAsync(problemId, null).Returns([]);

        await _sut.GetByProblemAsync(problemId, userId, isInstructor: true);

        await _submissionRepo.Received(1).GetByProblemAndUserAsync(problemId, null);
    }
}
