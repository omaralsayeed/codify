using Codify.Application.Agents;
using Codify.Application.DTOs.AI;
using Codify.Application.Interfaces;
using Codify.Application.Services;
using Codify.Domain.Entities;
using Codify.Domain.Enums;
using Codify.Domain.Exceptions;
using NSubstitute;

namespace Codify.Tests.Application;

public class AiHintServiceTests
{
    private readonly IProblemRepository _problemRepo = Substitute.For<IProblemRepository>();
    private readonly IHintRepository _hintRepo = Substitute.For<IHintRepository>();
    private readonly ITutorAgent _tutorAgent = Substitute.For<ITutorAgent>();
    private readonly AiHintService _sut;

    public AiHintServiceTests()
    {
        _sut = new AiHintService(_problemRepo, _hintRepo, _tutorAgent);
    }

    private static Problem MakeProblem()
    {
        var p = Problem.Create("Two Sum", "Given an array...", Difficulty.Easy, "", "[]");
        return p;
    }

    private static HintResponse MakeAgentResponse(int level) => new()
    {
        HintText = $"Hint at level {level}",
        HintLevel = level,
        HasMoreHints = level < 3
    };

    // ── POST /ai/hints ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetHintAsync_ShouldReturnLevel1_OnFirstRequest()
    {
        var problem = MakeProblem();
        var userId = Guid.NewGuid();

        _problemRepo.GetByIdWithDetailsAsync(problem.Id).Returns(problem);
        _hintRepo.GetCurrentHintLevelAsync(userId, problem.Id).Returns(0);
        _hintRepo.GetByUserAndProblemAsync(userId, problem.Id).Returns([]);
        _tutorAgent.GenerateHintAsync(Arg.Any<TutorAgentInput>(), Arg.Any<CancellationToken>())
            .Returns(MakeAgentResponse(1));

        var request = new HintRequest { ProblemId = problem.Id, StudentCode = "code", HintLevel = 1 };
        var result = await _sut.GetHintAsync(request, userId);

        Assert.Equal(1, result.HintLevel);
        Assert.True(result.HasMoreHints);
        await _hintRepo.Received(1).AddAsync(Arg.Is<HintLog>(h => h.HintLevel == 1 && h.UserId == userId));
        await _hintRepo.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task GetHintAsync_ShouldReturnLevel2_OnSecondRequest()
    {
        var problem = MakeProblem();
        var userId = Guid.NewGuid();

        _problemRepo.GetByIdWithDetailsAsync(problem.Id).Returns(problem);
        _hintRepo.GetCurrentHintLevelAsync(userId, problem.Id).Returns(1); // already got level 1
        _hintRepo.GetByUserAndProblemAsync(userId, problem.Id).Returns(
            [HintLog.Create(userId, problem.Id, 1, "First hint")]);
        _tutorAgent.GenerateHintAsync(Arg.Any<TutorAgentInput>(), Arg.Any<CancellationToken>())
            .Returns(MakeAgentResponse(2));

        var request = new HintRequest { ProblemId = problem.Id, StudentCode = "code", HintLevel = 2 };
        var result = await _sut.GetHintAsync(request, userId);

        Assert.Equal(2, result.HintLevel);
        Assert.True(result.HasMoreHints); // level 2 of 3, still more
    }

    [Fact]
    public async Task GetHintAsync_ShouldReturnHasMoreHintsFalse_AtLevel3()
    {
        var problem = MakeProblem();
        var userId = Guid.NewGuid();

        _problemRepo.GetByIdWithDetailsAsync(problem.Id).Returns(problem);
        _hintRepo.GetCurrentHintLevelAsync(userId, problem.Id).Returns(2);
        _hintRepo.GetByUserAndProblemAsync(userId, problem.Id).Returns([
            HintLog.Create(userId, problem.Id, 1, "H1"),
            HintLog.Create(userId, problem.Id, 2, "H2")
        ]);
        _tutorAgent.GenerateHintAsync(Arg.Any<TutorAgentInput>(), Arg.Any<CancellationToken>())
            .Returns(MakeAgentResponse(3));

        var request = new HintRequest { ProblemId = problem.Id, StudentCode = "code", HintLevel = 3 };
        var result = await _sut.GetHintAsync(request, userId);

        Assert.Equal(3, result.HintLevel);
        Assert.False(result.HasMoreHints); // at max
    }

    [Fact]
    public async Task GetHintAsync_ShouldThrowValidationException_WhenAlreadyAtMaxLevel()
    {
        var problem = MakeProblem();
        var userId = Guid.NewGuid();

        _problemRepo.GetByIdWithDetailsAsync(problem.Id).Returns(problem);
        _hintRepo.GetCurrentHintLevelAsync(userId, problem.Id).Returns(3); // already maxed

        var request = new HintRequest { ProblemId = problem.Id, StudentCode = "code", HintLevel = 1 };

        await Assert.ThrowsAsync<ValidationException>(() => _sut.GetHintAsync(request, userId));
        await _tutorAgent.DidNotReceive().GenerateHintAsync(Arg.Any<TutorAgentInput>(), Arg.Any<CancellationToken>());
        await _hintRepo.DidNotReceive().AddAsync(Arg.Any<HintLog>());
    }

    [Fact]
    public async Task GetHintAsync_ShouldThrowNotFoundException_WhenProblemDoesNotExist()
    {
        _problemRepo.GetByIdWithDetailsAsync(Arg.Any<Guid>()).Returns((Problem?)null);

        var request = new HintRequest { ProblemId = Guid.NewGuid(), StudentCode = "code", HintLevel = 1 };

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetHintAsync(request, Guid.NewGuid()));
    }

    [Fact]
    public async Task GetHintAsync_ShouldPassPreviousHintsToAgent()
    {
        var problem = MakeProblem();
        var userId = Guid.NewGuid();

        _problemRepo.GetByIdWithDetailsAsync(problem.Id).Returns(problem);
        _hintRepo.GetCurrentHintLevelAsync(userId, problem.Id).Returns(1);
        _hintRepo.GetByUserAndProblemAsync(userId, problem.Id).Returns(
            [HintLog.Create(userId, problem.Id, 1, "Think about hash maps")]);
        _tutorAgent.GenerateHintAsync(Arg.Any<TutorAgentInput>(), Arg.Any<CancellationToken>())
            .Returns(MakeAgentResponse(2));

        var request = new HintRequest { ProblemId = problem.Id, StudentCode = "code", HintLevel = 2 };
        await _sut.GetHintAsync(request, userId);

        await _tutorAgent.Received(1).GenerateHintAsync(
            Arg.Is<TutorAgentInput>(i => i.PreviousHints.Contains("Think about hash maps")),
            Arg.Any<CancellationToken>());
    }

    // ── GET /ai/hints/history ─────────────────────────────────────────────────

    [Fact]
    public async Task GetHintHistoryAsync_ShouldReturnEmptyHistory_WhenNoHintsUsed()
    {
        var userId = Guid.NewGuid();
        var problemId = Guid.NewGuid();

        _hintRepo.GetByUserAndProblemAsync(userId, problemId).Returns([]);

        var result = await _sut.GetHintHistoryAsync(problemId, userId);

        Assert.Equal(0, result.TotalHintsUsed);
        Assert.True(result.CanRequestMore);
        Assert.Empty(result.Hints);
    }

    [Fact]
    public async Task GetHintHistoryAsync_ShouldReturnAllHints_OrderedByLevel()
    {
        var userId = Guid.NewGuid();
        var problemId = Guid.NewGuid();

        _hintRepo.GetByUserAndProblemAsync(userId, problemId).Returns([
            HintLog.Create(userId, problemId, 1, "First hint"),
            HintLog.Create(userId, problemId, 2, "Second hint"),
            HintLog.Create(userId, problemId, 3, "Third hint")
        ]);

        var result = await _sut.GetHintHistoryAsync(problemId, userId);

        Assert.Equal(3, result.TotalHintsUsed);
        Assert.False(result.CanRequestMore); // all 3 used
        Assert.Equal(3, result.Hints.Count);
        Assert.Equal(1, result.Hints[0].HintLevel);
        Assert.Equal("First hint", result.Hints[0].HintText);
    }

    [Fact]
    public async Task GetHintHistoryAsync_ShouldReturnCanRequestMore_WhenBelowMax()
    {
        var userId = Guid.NewGuid();
        var problemId = Guid.NewGuid();

        _hintRepo.GetByUserAndProblemAsync(userId, problemId).Returns(
            [HintLog.Create(userId, problemId, 1, "First hint")]);

        var result = await _sut.GetHintHistoryAsync(problemId, userId);

        Assert.Equal(1, result.TotalHintsUsed);
        Assert.True(result.CanRequestMore);
    }
}
