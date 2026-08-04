using Codify.Application.Interfaces;
using Codify.Infrastructure.Search;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Codify.Tests.Infrastructure;

public class KnowledgeBaseSearchServiceTests
{
    private readonly IEmbeddingService _embedding = Substitute.For<IEmbeddingService>();
    private readonly IVectorStore _vectorStore = Substitute.For<IVectorStore>();
    private readonly KnowledgeBaseSearchService _sut;

    public KnowledgeBaseSearchServiceTests()
    {
        _sut = new KnowledgeBaseSearchService(_embedding, _vectorStore, NullLogger<KnowledgeBaseSearchService>.Instance);
    }

    [Fact]
    public async Task SearchAsync_EmbedsQuery_AndReturnsMappedConceptResults()
    {
        // Arrange
        var query = "how does memoization work";
        var vector = new[] { 0.1f, 0.2f, 0.3f };
        _embedding.GenerateAsync(query).Returns(vector);
        _vectorStore.SearchAsync(vector, "Dynamic Programming", "concept", 5, null)
            .Returns(new List<VectorSearchResult>
            {
                new()
                {
                    Id = "dp:1",
                    Content = "Memoization caches subproblem results.",
                    Similarity = 0.88f,
                    Metadata = new Dictionary<string, object> { ["concept_tag"] = "Dynamic Programming" }
                }
            });

        // Act
        var results = await _sut.SearchAsync(query, "Dynamic Programming");

        // Assert
        Assert.Single(results);
        Assert.Equal("Memoization caches subproblem results.", results[0].Content);
        Assert.Equal("Dynamic Programming", results[0].ConceptTag);
        Assert.Equal(0.88f, results[0].Relevance);
        await _vectorStore.Received(1).EnsureCollectionAsync();
    }

    [Fact]
    public async Task SearchAsync_WhenEmbeddingFails_ReturnsEmptyListAndLogsError()
    {
        // Arrange
        _embedding.GenerateAsync(Arg.Any<string>())
            .Returns(Task.FromException<float[]>(new InvalidOperationException("API down")));

        // Act
        var results = await _sut.SearchAsync("recursion base case");

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_WithEmptyQuery_ReturnsEmptyList()
    {
        // Act
        var results = await _sut.SearchAsync("   ");

        // Assert
        Assert.Empty(results);
        await _embedding.DidNotReceive().GenerateAsync(Arg.Any<string>());
    }
}
