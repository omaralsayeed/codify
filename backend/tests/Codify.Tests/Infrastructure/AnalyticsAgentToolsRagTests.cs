using Codify.Application.Interfaces;
using Codify.Infrastructure.AI;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Codify.Tests.Infrastructure;

public class AnalyticsAgentToolsRagTests
{
    private readonly ISubmissionRepository _submissionRepo = Substitute.For<ISubmissionRepository>();
    private readonly IVectorStore _vectorStore = Substitute.For<IVectorStore>();
    private readonly IEmbeddingService _embedding = Substitute.For<IEmbeddingService>();
    private readonly AnalyticsAgentTools _sut;

    public AnalyticsAgentToolsRagTests()
    {
        _sut = new AnalyticsAgentTools(
            _submissionRepo,
            _vectorStore,
            _embedding,
            NullLogger<AnalyticsAgentTools>.Instance);
    }

    [Fact]
    public async Task GetConceptContextAsync_EmbedsTopic_AndRetrievesConceptChunks()
    {
        var topic = "Dynamic Programming";
        var vector = new[] { 0.1f, 0.2f };
        _embedding.GenerateAsync(topic).Returns(vector);
        _vectorStore.SearchAsync(vector, topic, "concept", 3, 0.6f)
            .Returns(new List<VectorSearchResult>
            {
                new()
                {
                    Id = "concept:dp:0",
                    Content = "DP breaks problems into overlapping subproblems.",
                    Similarity = 0.91f,
                    Metadata = new Dictionary<string, object> { ["concept_tag"] = topic }
                }
            });

        var result = await _sut.GetConceptContextAsync(topic);

        Assert.Equal(topic, result.Topic);
        Assert.Single(result.RetrievedChunks);
        Assert.Contains("overlapping subproblems", result.RetrievedChunks[0]);
    }

    [Fact]
    public async Task GetConceptContextAsync_WhenSearchFails_ReturnsGracefulSummary()
    {
        _embedding.GenerateAsync(Arg.Any<string>())
            .Returns(Task.FromException<float[]>(new HttpRequestException("timeout")));

        var result = await _sut.GetConceptContextAsync("Graphs");

        Assert.Equal("Graphs", result.Topic);
        Assert.Contains("failed", result.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ClassifyProblemTagsAsync_EmbedsProblem_AndReturnsTop3TagsByVote()
    {
        var title = "Two Sum";
        var statement = "Given an array of integers, return indices of the two numbers that add up to target.";
        var vector = new[] { 0.5f, 0.6f };
        _embedding.GenerateAsync(Arg.Any<string>()).Returns(vector);
        _vectorStore.SearchAsync(vector, null, "problem", 5, 0.6f)
            .Returns(new List<VectorSearchResult>
            {
                new() { Id = "problem:1", Similarity = 0.85f, Metadata = new() { ["concept_tag"] = "Arrays & Hashing" } },
                new() { Id = "problem:2", Similarity = 0.80f, Metadata = new() { ["concept_tag"] = "Arrays & Hashing" } },
                new() { Id = "problem:3", Similarity = 0.75f, Metadata = new() { ["concept_tag"] = "Two Pointers" } }
            });

        var result = await _sut.ClassifyProblemTagsAsync(title, statement);

        Assert.Equal("Arrays & Hashing", result.SuggestedTags[0]);
        Assert.Equal("Two Pointers", result.SuggestedTags[1]);
        Assert.True(result.TagConfidences[0].Confidence > result.TagConfidences[1].Confidence);
    }

    [Fact]
    public async Task ClassifyProblemTagsAsync_WithEmptyStatement_ReturnsEmptyTags()
    {
        var result = await _sut.ClassifyProblemTagsAsync("Empty", "   ");

        Assert.Empty(result.SuggestedTags);
        Assert.Contains("No problem statement", result.Reasoning);
    }
}
