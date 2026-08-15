using Codify.Application.Interfaces;
using Codify.Infrastructure.Search;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Codify.Tests.Infrastructure;

/// <summary>
/// Tests for the RAG retrieval entry point. Verifies the query is embedded, results
/// are mapped to knowledge-base chunks, empty/missing retrieval returns empty, and
/// failures degrade gracefully (agents proceed without RAG context).
/// </summary>
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
    public async Task Search_ShouldEmbedQuery_AndReturnMappedChunks()
    {
        var vector = new[] { 0.1f, 0.2f };
        _embedding.GenerateAsync("hash map lookups", Arg.Any<CancellationToken>()).Returns(vector);
        _vectorStore.SearchAsync(vector, null, "concept", 5, null, Arg.Any<CancellationToken>())
            .Returns(new List<VectorSearchResult>
            {
                new()
                {
                    Id = "c1",
                    Content = "Hash maps give O(1) lookups.",
                    Similarity = 0.9f,
                    Metadata = new Dictionary<string, object> { ["concept_tag"] = "Arrays & Hashing" }
                }
            });

        var results = await _sut.SearchAsync("hash map lookups");

        Assert.Single(results);
        Assert.Equal("Hash maps give O(1) lookups.", results[0].Content);
        Assert.Equal("Arrays & Hashing", results[0].ConceptTag);
        Assert.Equal(0.9f, results[0].Relevance);
    }

    [Fact]
    public async Task Search_ShouldReturnEmpty_WhenQueryIsBlank()
    {
        var results = await _sut.SearchAsync("   ");

        Assert.Empty(results);
        await _embedding.DidNotReceive().GenerateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Search_ShouldReturnEmpty_WhenEmbeddingIsEmpty()
    {
        _embedding.GenerateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([]);

        var results = await _sut.SearchAsync("recursion");

        Assert.Empty(results);
    }

    [Fact]
    public async Task Search_ShouldReturnEmpty_WhenVectorStoreFails()
    {
        var vector = new[] { 0.1f };
        _embedding.GenerateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(vector);
        _vectorStore.SearchAsync(Arg.Any<float[]>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<int>(), Arg.Any<float?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<List<VectorSearchResult>>(new HttpRequestException("Chroma down")));

        var results = await _sut.SearchAsync("graphs");

        // Chroma outage must not throw; it returns empty so the agent proceeds.
        Assert.Empty(results);
    }

    [Fact]
    public async Task Search_ShouldReturnEmpty_WhenEmbeddingFails()
    {
        _embedding.GenerateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<float[]>(new InvalidOperationException("no key")));

        var results = await _sut.SearchAsync("dynamic programming");

        Assert.Empty(results);
    }
}
