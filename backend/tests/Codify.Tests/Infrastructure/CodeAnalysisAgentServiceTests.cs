using Codify.Application.Agents;
using Codify.Application.Interfaces;
using Codify.Domain.Enums;
using Codify.Infrastructure.AI;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Codify.Tests.Infrastructure;

/// <summary>
/// Tests for the Code Analysis Agent (static workflow). Verifies feedback parsing,
/// AI-generated detection (confidence threshold), and safe fallback on malformed
/// output or LLM failure.
/// </summary>
public class CodeAnalysisAgentServiceTests
{
    private readonly ILLMClient _llmClient = Substitute.For<ILLMClient>();
    private readonly IPromptLoader _promptLoader = Substitute.For<IPromptLoader>();
    private readonly CodeAnalysisAgentService _sut;

    public CodeAnalysisAgentServiceTests()
    {
        _promptLoader.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("You are the code analysis agent. {{code}} {{heuristics}}");
        _sut = new CodeAnalysisAgentService(_llmClient, _promptLoader, NullLogger<CodeAnalysisAgentService>.Instance);
    }

    private static CodeCheckerAgentInput MakeInput(string code) =>
        new(Guid.NewGuid(), code, "Python", "Two Sum", "Find two numbers.");

    private void LlmReturns(string json) =>
        _llmClient.CompleteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(json);

    [Fact]
    public async Task Analyze_ShouldReturnFeedbackItems_FromValidLlmOutput()
    {
        LlmReturns("""{"feedbackItems":[{"feedbackType":"CodeQuality","message":"Use better names."},{"feedbackType":"Optimization","message":"Use a hash map."}],"aiGenerated":false,"aiGeneratedConfidence":0.1,"aiGeneratedIndicators":""}""");

        var result = await _sut.AnalyzeAsync(MakeInput("def f(x): return x"));

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.FeedbackType == FeedbackType.CodeQuality);
        Assert.Contains(result, r => r.FeedbackType == FeedbackType.Optimization);
    }

    [Fact]
    public async Task Analyze_ShouldFlagAiGenerated_WhenConfident()
    {
        LlmReturns("""{"feedbackItems":[{"feedbackType":"CodeQuality","message":"Good."}],"aiGenerated":true,"aiGeneratedConfidence":0.85,"aiGeneratedIndicators":"Uniform style and explanatory comments."}""");

        var result = await _sut.AnalyzeAsync(MakeInput("def solution(nums): return nums"));

        Assert.Contains(result, r => r.FeedbackType == FeedbackType.AiGenerated);
    }

    [Fact]
    public async Task Analyze_ShouldNotFlagAiGenerated_WhenLowConfidence()
    {
        LlmReturns("""{"feedbackItems":[{"feedbackType":"CodeQuality","message":"Good."}],"aiGenerated":true,"aiGeneratedConfidence":0.2,"aiGeneratedIndicators":"weak"}""");

        var result = await _sut.AnalyzeAsync(MakeInput("def solution(nums): return nums"));

        Assert.DoesNotContain(result, r => r.FeedbackType == FeedbackType.AiGenerated);
    }

    [Fact]
    public async Task Analyze_ShouldFallback_WhenLlmReturnsInvalidJson()
    {
        LlmReturns("not valid json at all");

        var result = await _sut.AnalyzeAsync(MakeInput("def f(): pass"));

        Assert.Single(result);
        Assert.Equal(FeedbackType.CodeQuality, result[0].FeedbackType);
        Assert.Contains("temporarily unavailable", result[0].Message);
    }

    [Fact]
    public async Task Analyze_ShouldFallback_WhenLlmThrows()
    {
        _llmClient.CompleteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new InvalidOperationException("no key")));

        var result = await _sut.AnalyzeAsync(MakeInput("def f(): pass"));

        Assert.Single(result);
        Assert.Contains("temporarily unavailable", result[0].Message);
    }

    [Fact]
    public async Task Analyze_ShouldSkipUnknownFeedbackTypes()
    {
        LlmReturns("""{"feedbackItems":[{"feedbackType":"NotARealType","message":"x"},{"feedbackType":"CodeQuality","message":"ok"}],"aiGenerated":false,"aiGeneratedConfidence":0,"aiGeneratedIndicators":""}""");

        var result = await _sut.AnalyzeAsync(MakeInput("def f(): pass"));

        Assert.Single(result);
        Assert.Equal(FeedbackType.CodeQuality, result[0].FeedbackType);
    }
}
