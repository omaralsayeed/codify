using Codify.Application.Agents;
using Codify.Application.DTOs.AI;
using Codify.Application.Interfaces;
using Codify.Infrastructure.AI;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Codify.Tests.Infrastructure;

/// <summary>
/// Tests for the agentic Tutor Agent's tool-calling loop. The model decides which
/// tools to call; the service executes them, appends results, and resends until the
/// model returns final JSON. We verify termination, the iteration cap, tool tracking,
/// hint-level clamping, and safe fallback on malformed output.
/// </summary>
public class TutorAgentServiceTests
{
    private readonly ILLMClient _llmClient = Substitute.For<ILLMClient>();
    private readonly ITutorAgentTools _tools = Substitute.For<ITutorAgentTools>();
    private readonly IPromptLoader _promptLoader = Substitute.For<IPromptLoader>();
    private readonly TutorAgentService _sut;

    public TutorAgentServiceTests()
    {
        _promptLoader.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("You are the tutor agent.");
        _sut = new TutorAgentService(_llmClient, _tools, _promptLoader, NullLogger<TutorAgentService>.Instance);
    }

    private static TutorAgentInput MakeInput() => new()
    {
        UserId = Guid.NewGuid(),
        ProblemId = Guid.NewGuid(),
        ProblemTitle = "Two Sum",
        ProblemStatement = "Find two numbers that add to target.",
        HintLevel = 1,
        StudentCode = "def f(): pass"
    };

    private static LlmResponse FinalText(int level) => new()
    {
        FinalText = $"{{\"hintText\":\"Think about lookups.\",\"hintLevel\":{level},\"followUpQuestion\":\"What key?\",\"hasMoreHints\":true,\"reasoningSummary\":\"test\"}}"
    };

    [Fact]
    public async Task GenerateHint_ShouldReturnParsedHint_WhenModelAnswersDirectly()
    {
        _llmClient.CompleteWithToolsAsync(Arg.Any<IReadOnlyList<LlmMessage>>(), Arg.Any<IReadOnlyList<LlmToolDefinition>>(), Arg.Any<CancellationToken>())
            .Returns(FinalText(1));

        var result = await _sut.GenerateHintAsync(MakeInput());

        Assert.Equal("Think about lookups.", result.HintText);
        Assert.Equal(1, result.HintLevel);
        Assert.Empty(result.ToolsUsed);
    }

    [Fact]
    public async Task GenerateHint_ShouldExecuteRequestedTool_ThenReturnFinalHint()
    {
        _tools.GetAttemptHistoryAsync(Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new AttemptHistoryResult { AttemptCount = 2 });

        var toolCallResponse = new LlmResponse
        {
            ToolCalls = [new LlmToolCall { Id = "call_1", Name = "get_attempt_history", ArgumentsJson = "{}" }]
        };

        _llmClient.CompleteWithToolsAsync(Arg.Any<IReadOnlyList<LlmMessage>>(), Arg.Any<IReadOnlyList<LlmToolDefinition>>(), Arg.Any<CancellationToken>())
            .Returns(toolCallResponse, FinalText(2));

        var result = await _sut.GenerateHintAsync(MakeInput());

        // The model-requested tool was executed and recorded as evidence.
        await _tools.Received(1).GetAttemptHistoryAsync(Arg.Any<Guid>(), Arg.Any<Guid>());
        Assert.Contains("get_attempt_history", result.ToolsUsed);
        Assert.Equal("Think about lookups.", result.HintText);
    }

    [Fact]
    public async Task GenerateHint_ShouldFallBack_WhenIterationCapReached()
    {
        var toolCallResponse = new LlmResponse
        {
            ToolCalls = [new LlmToolCall { Id = "call_1", Name = "get_attempt_history", ArgumentsJson = "{}" }]
        };
        _tools.GetAttemptHistoryAsync(Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new AttemptHistoryResult());

        // Model keeps requesting tools and never produces a final answer.
        _llmClient.CompleteWithToolsAsync(Arg.Any<IReadOnlyList<LlmMessage>>(), Arg.Any<IReadOnlyList<LlmToolDefinition>>(), Arg.Any<CancellationToken>())
            .Returns(toolCallResponse);

        var result = await _sut.GenerateHintAsync(MakeInput());

        // Falls back safely after 5 iterations.
        Assert.Contains("reviewing the problem constraints", result.HintText);
        Assert.Contains("get_attempt_history", result.ToolsUsed);
    }

    [Fact]
    public async Task GenerateHint_ShouldFallBack_WhenModelReturnsInvalidJson()
    {
        _llmClient.CompleteWithToolsAsync(Arg.Any<IReadOnlyList<LlmMessage>>(), Arg.Any<IReadOnlyList<LlmToolDefinition>>(), Arg.Any<CancellationToken>())
            .Returns(new LlmResponse { FinalText = "this is not json" });

        var result = await _sut.GenerateHintAsync(MakeInput());

        Assert.Contains("reviewing the problem constraints", result.HintText);
    }

    [Fact]
    public async Task GenerateHint_ShouldClampHintLevel_ToValidRange()
    {
        _llmClient.CompleteWithToolsAsync(Arg.Any<IReadOnlyList<LlmMessage>>(), Arg.Any<IReadOnlyList<LlmToolDefinition>>(), Arg.Any<CancellationToken>())
            .Returns(FinalText(9)); // out of range

        var result = await _sut.GenerateHintAsync(MakeInput());

        Assert.Equal(3, result.HintLevel); // clamped to max
    }

    [Fact]
    public async Task GenerateHint_ShouldFallBack_WhenLlmThrows()
    {
        _llmClient.CompleteWithToolsAsync(Arg.Any<IReadOnlyList<LlmMessage>>(), Arg.Any<IReadOnlyList<LlmToolDefinition>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<LlmResponse>(new InvalidOperationException("no key")));

        var result = await _sut.GenerateHintAsync(MakeInput());

        Assert.Contains("reviewing the problem constraints", result.HintText);
        Assert.Equal("Fallback: LLM call failed or returned invalid response.", result.ReasoningSummary);
    }
}
