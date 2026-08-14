using Codify.Application.Agents;
using Codify.Application.Interfaces;
using Codify.Infrastructure.AI;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Codify.Tests.Infrastructure;

/// <summary>
/// Tests for the Tagging Agent (static workflow). Verifies that assigned tags are
/// validated against the allowed list, RAG grounding is used, and failures degrade
/// gracefully to an empty classification.
/// </summary>
public class TaggingAgentServiceTests
{
    private readonly ILLMClient _llmClient = Substitute.For<ILLMClient>();
    private readonly IPromptLoader _promptLoader = Substitute.For<IPromptLoader>();
    private readonly IKnowledgeBaseSearchService _knowledgeBase = Substitute.For<IKnowledgeBaseSearchService>();
    private readonly TaggingAgentService _sut;

    public TaggingAgentServiceTests()
    {
        _promptLoader.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Classify tags. {{availableTags}} {{retrievedContext}} {{problemTitle}} {{problemStatement}}");
        _knowledgeBase.SearchAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<KnowledgeBaseResult>
            {
                new() { Content = "Hash maps give O(1) lookups.", ConceptTag = "Arrays & Hashing", Relevance = 0.9f }
            });
        _sut = new TaggingAgentService(_llmClient, _promptLoader, _knowledgeBase, NullLogger<TaggingAgentService>.Instance);
    }

    private static TaggingAgentInput MakeInput(params string[] availableTags) => new()
    {
        ProblemTitle = "Two Sum",
        ProblemStatement = "Find two numbers that add to target.",
        AvailableTags = availableTags.ToList()
    };

    private void LlmReturns(string json) =>
        _llmClient.CompleteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(json);

    [Fact]
    public async Task Classify_ShouldReturnAllowedTags_FromValidOutput()
    {
        LlmReturns("""{"assignedTags":["Arrays & Hashing","Sorting"],"confidence":0.8,"reasoning":"lookup + ordering"}""");

        var result = await _sut.ClassifyProblemTagsAsync(MakeInput("Arrays & Hashing", "Sorting", "Graphs"));

        Assert.Equal(2, result.AssignedTags.Count);
        Assert.Contains("Arrays & Hashing", result.AssignedTags);
        Assert.Equal(0.8, result.Confidence);
    }

    [Fact]
    public async Task Classify_ShouldFilterOutTags_NotInAllowedList()
    {
        LlmReturns("""{"assignedTags":["Quantum Computing","Graphs"],"confidence":0.7,"reasoning":"x"}""");

        var result = await _sut.ClassifyProblemTagsAsync(MakeInput("Graphs", "Trees"));

        // "Quantum Computing" is not allowed and must be dropped.
        Assert.Single(result.AssignedTags);
        Assert.Equal("Graphs", result.AssignedTags[0]);
    }

    [Fact]
    public async Task Classify_ShouldReturnEmpty_WhenNoTagsAreAllowed()
    {
        var result = await _sut.ClassifyProblemTagsAsync(MakeInput());

        Assert.Empty(result.AssignedTags);
        Assert.Contains("No available tags", result.Reasoning);
    }

    [Fact]
    public async Task Classify_ShouldUseRagContext_AndStillClassify_WhenRetrievalFails()
    {
        _knowledgeBase.SearchAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<List<KnowledgeBaseResult>>(new HttpRequestException("Chroma down")));
        LlmReturns("""{"assignedTags":["Graphs"],"confidence":0.6,"reasoning":"x"}""");

        var result = await _sut.ClassifyProblemTagsAsync(MakeInput("Graphs"));

        // RAG failure does not block classification.
        Assert.Single(result.AssignedTags);
    }

    [Fact]
    public async Task Classify_ShouldReturnEmpty_WhenLlmReturnsInvalidJson()
    {
        LlmReturns("garbage not json");

        var result = await _sut.ClassifyProblemTagsAsync(MakeInput("Graphs"));

        Assert.Empty(result.AssignedTags);
    }

    [Fact]
    public async Task Classify_ShouldReturnEmpty_WhenLlmThrows()
    {
        _llmClient.CompleteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new InvalidOperationException("no key")));

        var result = await _sut.ClassifyProblemTagsAsync(MakeInput("Graphs"));

        Assert.Empty(result.AssignedTags);
        Assert.Contains("LLM call failed", result.Reasoning);
    }
}
