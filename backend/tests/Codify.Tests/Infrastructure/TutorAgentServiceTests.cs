using Codify.Application.Agents;
using Codify.Application.DTOs.AI;
using Codify.Application.Interfaces;
using Codify.Infrastructure.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Codify.Tests.Infrastructure;

public class TutorAgentServiceTests
{
    private readonly ILLMClient _llmClient = Substitute.For<ILLMClient>();
    private readonly ITutorAgentTools _tools = Substitute.For<ITutorAgentTools>();
    private readonly IPromptLoader _promptLoader = Substitute.For<IPromptLoader>();
    private readonly OpenAiOptions _options = new()
    {
        Model = "gpt-4o-mini",
        EscalationModel = "gpt-4o",
        EscalationAttemptThreshold = 3,
        EscalationHintLevelThreshold = 2
    };
    private readonly TutorAgentService _sut;

    public TutorAgentServiceTests()
    {
        _promptLoader.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("You are the tutor agent.");
        _sut = new TutorAgentService(
            _llmClient, _tools, _promptLoader,
            Options.Create(_options),
            NullLogger<TutorAgentService>.Instance);
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
        FinalText = $"{{\"hintText\":\"Think about lookups.\",\"hintLevel\":{level},\"followUpQuestion\":\"What key?\",\"hasMoreHints\":true,\"reasoningSummary\":\"test\"}}",
        ModelUsed = "gpt-4o-mini",
        TotalTokens = 50
    };

    [Fact]
    public async Task GenerateHint_ShouldReturnParsedHint_WhenModelAnswersDirectly()
    {
        _llmClient.CompleteWithToolsAsync(
                Arg.Any<IReadOnlyList<LlmMessage>>(), Arg.Any<IReadOnlyList<LlmToolDefinition>>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(FinalText(1));
        var result = await _sut.GenerateHintAsync(MakeInput());
        Assert.Equal("Think about lookups.", result.HintText);
        Assert.Equal(1, result.HintLevel);
    }

    [Fact]
    public async Task GenerateHint_ShouldExecuteRequestedTool_ThenReturnFinalHint()
    {
        _tools.GetAttemptHistoryAsync(Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new AttemptHistoryResult { AttemptCount = 2 });
        var toolCallResponse = new LlmResponse
        {
            ToolCalls = [new LlmToolCall { Id = "call_1", Name = "get_attempt_history", ArgumentsJson = "{}" }],
            ModelUsed = "gpt-4o-mini", TotalTokens = 30
        };
        _llmClient.CompleteWithToolsAsync(
                Arg.Any<IReadOnlyList<LlmMessage>>(), Arg.Any<IReadOnlyList<LlmToolDefinition>>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(toolCallResponse, FinalText(2));
        var result = await _sut.GenerateHintAsync(MakeInput());
        await _tools.Received(1).GetAttemptHistoryAsync(Arg.Any<Guid>(), Arg.Any<Guid>());
        Assert.Contains("get_attempt_history", result.ToolsUsed);
    }

    [Fact]
    public async Task GenerateHint_ShouldFallBack_WhenIterationCapReached()
    {
        var toolCallResponse = new LlmResponse
        {
            ToolCalls = [new LlmToolCall { Id = "call_1", Name = "get_attempt_history", ArgumentsJson = "{}" }],
            ModelUsed = "gpt-4o-mini", TotalTokens = 20
        };
        _tools.GetAttemptHistoryAsync(Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new AttemptHistoryResult());
        _llmClient.CompleteWithToolsAsync(
                Arg.Any<IReadOnlyList<LlmMessage>>(), Arg.Any<IReadOnlyList<LlmToolDefinition>>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(toolCallResponse);
        var result = await _sut.GenerateHintAsync(MakeInput());
        Assert.Contains("reviewing the problem constraints", result.HintText);
    }

    [Fact]
    public async Task GenerateHint_ShouldFallBack_WhenModelReturnsInvalidJson()
    {
        _llmClient.CompleteWithToolsAsync(
                Arg.Any<IReadOnlyList<LlmMessage>>(), Arg.Any<IReadOnlyList<LlmToolDefinition>>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new LlmResponse { FinalText = "this is not json", ModelUsed = "gpt-4o-mini", TotalTokens = 10 });
        var result = await _sut.GenerateHintAsync(MakeInput());
        Assert.Contains("reviewing the problem constraints", result.HintText);
    }

    [Fact]
    public async Task GenerateHint_ShouldClampHintLevel_ToValidRange()
    {
        _llmClient.CompleteWithToolsAsync(
                Arg.Any<IReadOnlyList<LlmMessage>>(), Arg.Any<IReadOnlyList<LlmToolDefinition>>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(FinalText(9));
        var result = await _sut.GenerateHintAsync(MakeInput());
        Assert.Equal(3, result.HintLevel);
    }

    [Fact]
    public async Task GenerateHint_ShouldFallBack_WhenLlmThrows()
    {
        _llmClient.CompleteWithToolsAsync(
                Arg.Any<IReadOnlyList<LlmMessage>>(), Arg.Any<IReadOnlyList<LlmToolDefinition>>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<LlmResponse>(new InvalidOperationException("no key")));
        var result = await _sut.GenerateHintAsync(MakeInput());
        Assert.Contains("reviewing the problem constraints", result.HintText);
    }

    [Fact]
    public async Task GenerateHint_ShouldUseDefaultModel_WhenAttemptCountIsLow()
    {
        var input = MakeInput();
        input.AttemptCount = 1;
        input.HintLevel = 1;
        _llmClient.CompleteWithToolsAsync(
                Arg.Any<IReadOnlyList<LlmMessage>>(), Arg.Any<IReadOnlyList<LlmToolDefinition>>(),
                Arg.Is<string>(m => m == "gpt-4o-mini"), Arg.Any<CancellationToken>())
            .Returns(FinalText(1));
        var result = await _sut.GenerateHintAsync(input);
        Assert.Equal("gpt-4o-mini", result.ModelUsed);
    }

    [Fact]
    public async Task GenerateHint_ShouldEscalateToExpensiveModel_WhenAttemptCountAndHintLevelAreHigh()
    {
        var input = MakeInput();
        input.AttemptCount = 5;
        input.HintLevel = 2;
        var response = FinalText(2);
        response.ModelUsed = "gpt-4o";
        _llmClient.CompleteWithToolsAsync(
                Arg.Any<IReadOnlyList<LlmMessage>>(), Arg.Any<IReadOnlyList<LlmToolDefinition>>(),
                Arg.Is<string>(m => m == "gpt-4o"), Arg.Any<CancellationToken>())
            .Returns(response);
        var result = await _sut.GenerateHintAsync(input);
        Assert.Equal("gpt-4o", result.ModelUsed);
    }

    [Fact]
    public async Task GenerateHint_ShouldAccumulateTokens_AcrossIterations()
    {
        var toolCallResponse = new LlmResponse
        {
            ToolCalls = [new LlmToolCall { Id = "call_1", Name = "get_attempt_history", ArgumentsJson = "{}" }],
            ModelUsed = "gpt-4o-mini", TotalTokens = 100
        };
        var finalResponse = FinalText(1);
        finalResponse.TotalTokens = 200;
        _tools.GetAttemptHistoryAsync(Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new AttemptHistoryResult());
        _llmClient.CompleteWithToolsAsync(
                Arg.Any<IReadOnlyList<LlmMessage>>(), Arg.Any<IReadOnlyList<LlmToolDefinition>>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(toolCallResponse, finalResponse);
        var result = await _sut.GenerateHintAsync(MakeInput());
        Assert.Equal(300, result.TotalTokens);
    }
}
