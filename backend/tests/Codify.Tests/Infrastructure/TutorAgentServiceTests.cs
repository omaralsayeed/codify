using System.Text.Json;
using Codify.Application.Agents;
using Codify.Application.DTOs.AI;
using Codify.Application.Interfaces;
using Codify.Infrastructure.AI;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Codify.Tests.Infrastructure;

public class TutorAgentServiceTests
{
    private readonly ILLMClient _llmClient = Substitute.For<ILLMClient>();
    private readonly ITutorAgentTools _tools = Substitute.For<ITutorAgentTools>();
    private readonly IPromptLoader _promptLoader = Substitute.For<IPromptLoader>();
    private readonly TutorAgentService _sut;

    public TutorAgentServiceTests()
    {
        _sut = new TutorAgentService(
            _llmClient,
            _tools,
            _promptLoader,
            NullLogger<TutorAgentService>.Instance);

        _promptLoader.LoadAsync("tutor-agent-system.txt", Arg.Any<CancellationToken>())
            .Returns("You are the Tutor Agent.");
    }

    [Fact]
    public async Task GenerateHintAsync_ShouldReturnFinalHintWithoutTools_WhenModelAnswersDirectly()
    {
        var input = CreateInput();
        var finalJson = JsonSerializer.Serialize(new HintResponse
        {
            HintText = "Think about the constraints.",
            HintLevel = 1,
            ToolsUsed = [],
            ReasoningSummary = "No tools needed."
        });

        _llmClient.CompleteWithToolsAsync(Arg.Any<List<LlmMessage>>(), Arg.Any<List<LlmToolDefinition>>(), Arg.Any<CancellationToken>())
            .Returns(new LlmResponse { FinalText = finalJson });

        var result = await _sut.GenerateHintAsync(input);

        Assert.Equal("Think about the constraints.", result.HintText);
        Assert.Equal(1, result.HintLevel);
        Assert.Empty(result.ToolsUsed ?? []);
    }

    [Fact]
    public async Task GenerateHintAsync_ShouldExecuteToolCalls_AndResendUntilFinalAnswer()
    {
        var input = CreateInput();
        var toolCall = new LlmToolCall
        {
            Id = "call_1",
            Name = "get_attempt_history",
            ArgumentsJson = $"{{\"studentId\":\"{input.UserId}\",\"problemId\":\"{input.ProblemId}\"}}"
        };

        _llmClient.CompleteWithToolsAsync(Arg.Any<List<LlmMessage>>(), Arg.Any<List<LlmToolDefinition>>(), Arg.Any<CancellationToken>())
            .Returns(
                new LlmResponse { HasToolCalls = true, ToolCalls = [toolCall] },
                new LlmResponse
                {
                    FinalText = JsonSerializer.Serialize(new HintResponse
                    {
                        HintText = "You have tried twice. Focus on the edge case when the array is empty.",
                        HintLevel = 2,
                        ToolsUsed = ["get_attempt_history"],
                        ReasoningSummary = "Used attempt history to escalate specificity."
                    })
                });

        _tools.GetAttemptHistoryAsync(input.UserId, input.ProblemId)
            .Returns(new AttemptHistoryResult
            {
                AttemptCount = 2,
                SubmissionStatuses = ["WrongAnswer", "WrongAnswer"],
                PreviousHintLevels = [1],
                Timestamps = []
            });

        var result = await _sut.GenerateHintAsync(input);

        Assert.Equal("You have tried twice. Focus on the edge case when the array is empty.", result.HintText);
        Assert.Equal(2, result.HintLevel);
        Assert.Contains("get_attempt_history", result.ToolsUsed ?? []);
        await _tools.Received(1).GetAttemptHistoryAsync(input.UserId, input.ProblemId);
    }

    [Fact]
    public async Task GenerateHintAsync_ShouldCapIterations_AndReturnFallback()
    {
        var input = CreateInput();
        var toolCall = new LlmToolCall
        {
            Id = "call_x",
            Name = "get_attempt_history",
            ArgumentsJson = $"{{\"studentId\":\"{input.UserId}\",\"problemId\":\"{input.ProblemId}\"}}"
        };

        // Model always returns the same tool call, never a final answer.
        _llmClient.CompleteWithToolsAsync(Arg.Any<List<LlmMessage>>(), Arg.Any<List<LlmToolDefinition>>(), Arg.Any<CancellationToken>())
            .Returns(new LlmResponse { HasToolCalls = true, ToolCalls = [toolCall] });

        _tools.GetAttemptHistoryAsync(Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new AttemptHistoryResult { AttemptCount = 0 });

        var result = await _sut.GenerateHintAsync(input);

        Assert.NotNull(result.HintText);
        Assert.NotEmpty(result.HintText);
        Assert.Contains("get_attempt_history", result.ToolsUsed ?? []);
        await _tools.Received(5).GetAttemptHistoryAsync(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    [Fact]
    public async Task GenerateHintAsync_ShouldClampHintLevel_WhenModelReturnsOutOfRange()
    {
        var input = CreateInput();
        var finalJson = JsonSerializer.Serialize(new HintResponse
        {
            HintText = "Here is a hint.",
            HintLevel = 99,
            ToolsUsed = [],
            ReasoningSummary = "Test clamping."
        });

        _llmClient.CompleteWithToolsAsync(Arg.Any<List<LlmMessage>>(), Arg.Any<List<LlmToolDefinition>>(), Arg.Any<CancellationToken>())
            .Returns(new LlmResponse { FinalText = finalJson });

        var result = await _sut.GenerateHintAsync(input);

        Assert.Equal(HintRequest.MaxHintLevel, result.HintLevel);
    }

    private static TutorAgentInput CreateInput() => new()
    {
        UserId = Guid.NewGuid(),
        ProblemId = Guid.NewGuid(),
        ProblemTitle = "Two Sum",
        ProblemStatement = "Find two numbers that add up to target.",
        ConceptTags = ["Arrays & Hashing"],
        HintLevel = 1,
        StudentCode = null,
        Language = "Python",
        LastSubmissionStatus = null
    };
}
