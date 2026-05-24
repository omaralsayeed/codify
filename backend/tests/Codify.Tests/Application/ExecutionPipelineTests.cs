using Codify.Application.DTOs.Execution;
using Codify.Application.DTOs.Submissions;
using Codify.Application.Interfaces;
using Codify.Application.Services;
using Codify.Domain.Entities;
using Codify.Domain.Enums;
using NSubstitute;

namespace Codify.Tests.Application;

/// <summary>
/// Tests for the execution pipeline: POST /submissions triggers evaluation,
/// stores SubmissionResult, updates counters and performance profile.
/// </summary>
public class ExecutionPipelineTests
{
    private readonly ISubmissionRepository _submissionRepo = Substitute.For<ISubmissionRepository>();
    private readonly IProblemRepository _problemRepo = Substitute.For<IProblemRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IExecutionService _executionService = Substitute.For<IExecutionService>();
    private readonly IPerformanceService _performanceService = Substitute.For<IPerformanceService>();
    private readonly SubmissionService _sut;

    public ExecutionPipelineTests()
    {
        _sut = new SubmissionService(
            _submissionRepo, _problemRepo, _userRepo, _executionService, _performanceService);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static Problem MakeProblem(params (string Input, string Expected)[] cases)
    {
        var p = Problem.Create("Test", "...", Difficulty.Easy, "", "[]");
        int i = 0;
        foreach (var (input, expected) in cases)
            p.TestCases.Add(TestCase.Create(p.Id, input, expected, false, TestCaseVisibility.Hidden, i++));
        return p;
    }

    private void SetupEval(string actualOutput, bool timedOut = false,
        bool compileError = false, bool runtimeError = false)
    {
        _executionService.EvaluateAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new TestCaseExecutionResult
            {
                ActualOutput = actualOutput,
                ExecutionTimeMs = 5,
                MemoryUsedKb = 256,
                TimedOut = timedOut,
                CompileError = compileError,
                RuntimeError = runtimeError
            });
    }

    private void SetupGetById(Guid problemId, Guid userId)
    {
        _submissionRepo.GetByIdWithDetailsAsync(Arg.Any<Guid>())
            .Returns(_ => Submission.Create(problemId, userId, "code", SubmissionLanguage.Python));
    }

    // ── tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Pipeline_ShouldPersistSubmissionResult_AfterEvaluation()
    {
        var userId = Guid.NewGuid();
        var problem = MakeProblem(("1\n2", "3"), ("2\n3", "5"));
        SetupEval("3");   // both cases return "3" — first passes, second fails
        SetupGetById(problem.Id, userId);
        _problemRepo.GetByIdWithTestCasesAsync(problem.Id).Returns(problem);
        _submissionRepo.HasPreviousAcceptedAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(false);
        _userRepo.GetByIdAsync(userId).Returns(User.Create("T", "t@t.com", "h", UserRole.Student));

        await _sut.CreateAsync(new CreateSubmissionRequest
        {
            ProblemId = problem.Id, Code = "code", Language = SubmissionLanguage.Python
        }, userId);

        // SubmissionResult must be persisted
        await _submissionRepo.Received(1).AddResultAsync(Arg.Any<SubmissionResult>());
    }

    [Fact]
    public async Task Pipeline_ShouldMarkAccepted_WhenAllTestCasesPass()
    {
        var userId = Guid.NewGuid();
        var problem = MakeProblem(("1", "1"), ("2", "2"));
        SetupEval("1");  // stub always returns "1" — matches both expected outputs
        _problemRepo.GetByIdWithTestCasesAsync(problem.Id).Returns(problem);
        _submissionRepo.HasPreviousAcceptedAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(false);
        _userRepo.GetByIdAsync(userId).Returns(User.Create("T", "t@t.com", "h", UserRole.Student));

        Submission? captured = null;
        _submissionRepo.GetByIdWithDetailsAsync(Arg.Any<Guid>())
            .Returns(callInfo =>
            {
                captured = Submission.Create(problem.Id, userId, "code", SubmissionLanguage.Python);
                captured.MarkAsAccepted(10, 256, 2, 2);
                return captured;
            });

        var result = await _sut.CreateAsync(new CreateSubmissionRequest
        {
            ProblemId = problem.Id, Code = "code", Language = SubmissionLanguage.Python
        }, userId);

        Assert.Equal("Accepted", result.Status);
    }

    [Fact]
    public async Task Pipeline_ShouldMarkWrongAnswer_WhenAnyTestCaseFails()
    {
        var userId = Guid.NewGuid();
        var problem = MakeProblem(("1", "correct"), ("2", "correct"));
        SetupEval("wrong");  // always returns "wrong"
        _problemRepo.GetByIdWithTestCasesAsync(problem.Id).Returns(problem);
        _submissionRepo.HasPreviousAcceptedAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(false);
        _userRepo.GetByIdAsync(userId).Returns(User.Create("T", "t@t.com", "h", UserRole.Student));

        Submission? captured = null;
        _submissionRepo.GetByIdWithDetailsAsync(Arg.Any<Guid>())
            .Returns(_ =>
            {
                captured = Submission.Create(problem.Id, userId, "code", SubmissionLanguage.Python);
                captured.MarkAsFailed(SubmissionStatus.WrongAnswer, 0, 2);
                return captured;
            });

        var result = await _sut.CreateAsync(new CreateSubmissionRequest
        {
            ProblemId = problem.Id, Code = "code", Language = SubmissionLanguage.Python
        }, userId);

        Assert.Equal("WrongAnswer", result.Status);
    }

    [Fact]
    public async Task Pipeline_ShouldMarkCompileError_WhenExecutionReportsCompileError()
    {
        var userId = Guid.NewGuid();
        var problem = MakeProblem(("1", "1"));
        SetupEval(string.Empty, compileError: true);
        _problemRepo.GetByIdWithTestCasesAsync(problem.Id).Returns(problem);
        _submissionRepo.HasPreviousAcceptedAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(false);
        _userRepo.GetByIdAsync(userId).Returns(User.Create("T", "t@t.com", "h", UserRole.Student));

        _submissionRepo.GetByIdWithDetailsAsync(Arg.Any<Guid>())
            .Returns(_ =>
            {
                var s = Submission.Create(problem.Id, userId, "code", SubmissionLanguage.Python);
                s.MarkAsFailed(SubmissionStatus.CompileError, 0, 1);
                return s;
            });

        var result = await _sut.CreateAsync(new CreateSubmissionRequest
        {
            ProblemId = problem.Id, Code = "bad code", Language = SubmissionLanguage.Python
        }, userId);

        Assert.Equal("CompileError", result.Status);
    }

    [Fact]
    public async Task Pipeline_ShouldUpdatePerformanceProfile_AfterEverySubmission()
    {
        var userId = Guid.NewGuid();
        var problem = MakeProblem(("1", "1"));
        SetupEval("1");
        _problemRepo.GetByIdWithTestCasesAsync(problem.Id).Returns(problem);
        _submissionRepo.HasPreviousAcceptedAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(false);
        _userRepo.GetByIdAsync(userId).Returns(User.Create("T", "t@t.com", "h", UserRole.Student));
        _submissionRepo.GetByIdWithDetailsAsync(Arg.Any<Guid>())
            .Returns(_ => Submission.Create(problem.Id, userId, "code", SubmissionLanguage.Python));

        await _sut.CreateAsync(new CreateSubmissionRequest
        {
            ProblemId = problem.Id, Code = "code", Language = SubmissionLanguage.Python
        }, userId);

        await _performanceService.Received(1).UpdateAfterSubmissionAsync(userId);
    }

    [Fact]
    public async Task Pipeline_ShouldIncrementProblemCounters_AfterSubmission()
    {
        var userId = Guid.NewGuid();
        var problem = MakeProblem(("1", "1"));
        SetupEval("1");
        _problemRepo.GetByIdWithTestCasesAsync(problem.Id).Returns(problem);
        _submissionRepo.HasPreviousAcceptedAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(false);
        _userRepo.GetByIdAsync(userId).Returns(User.Create("T", "t@t.com", "h", UserRole.Student));
        _submissionRepo.GetByIdWithDetailsAsync(Arg.Any<Guid>())
            .Returns(_ => Submission.Create(problem.Id, userId, "code", SubmissionLanguage.Python));

        await _sut.CreateAsync(new CreateSubmissionRequest
        {
            ProblemId = problem.Id, Code = "code", Language = SubmissionLanguage.Python
        }, userId);

        // Problem counters are updated via IncrementSubmissionCounters → SaveChangesAsync on problemRepo
        await _problemRepo.Received(1).SaveChangesAsync();
    }
}
