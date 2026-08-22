using System.Net;
using Codify.Application.Interfaces;
using Codify.Infrastructure.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Codify.Tests.Infrastructure;

/// <summary>
/// Tests for the Chroma Cloud vector store client. Uses a fake HTTP handler to
/// verify collection resolution, batch query-response parsing, similarity
/// thresholding, metadata filtering, and graceful failure handling.
/// </summary>
public class ChromaCloudVectorStoreTests
{
    private static ChromaCloudVectorStore MakeStore(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handler = new FakeHttpMessageHandler(responder);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.trychroma.com/") };
        
        // Configure headers as done in production DependencyInjection
        httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-key");
        httpClient.DefaultRequestHeaders.Add("x-chroma-token", "test-key");
        
        var options = Options.Create(new ChromaCloudOptions
        {
            Endpoint = "https://api.trychroma.com",
            ApiKey = "test-key",
            Tenant = "tenant",
            Database = "db",
            CollectionName = "codify-knowledge-base",
            SimilarityThreshold = 0.25f
        });
        return new ChromaCloudVectorStore(httpClient, options, NullLogger<ChromaCloudVectorStore>.Instance);
    }

    private static HttpResponseMessage CollectionResponse() =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"id":"col-123","name":"codify-knowledge-base"}""")
        };

    [Fact]
    public async Task Search_ShouldParseBatchQueryResponse()
    {
        var queryJson = """
        {
          "ids": [["id1","id2"]],
          "documents": [["chunk one","chunk two"]],
          "metadatas": [[{"concept_tag":"Graphs"},{"concept_tag":"Trees"}]],
          "distances": [[0.5,1.0]]
        }
        """;

        var store = MakeStore(req => req.Method == HttpMethod.Get
            ? CollectionResponse()
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(queryJson) });

        var results = await store.SearchAsync(new[] { 0.1f, 0.2f }, topK: 5);

        Assert.Equal(2, results.Count);
        Assert.Equal("chunk one", results[0].Content);
        Assert.Equal("Graphs", results[0].Metadata["concept_tag"].ToString());
        // distance 0.5 -> similarity 1/(1+0.5) ~= 0.667; distance 1.0 -> 0.5
        Assert.True(results[0].Similarity > results[1].Similarity);
    }

    [Fact]
    public async Task Search_ShouldFilterResults_BelowSimilarityThreshold()
    {
        // distance 4.0 -> similarity 0.2, below the 0.25 threshold.
        var queryJson = """
        {
          "ids": [["id1"]],
          "documents": [["far away chunk"]],
          "metadatas": [[{"concept_tag":"Graphs"}]],
          "distances": [[4.0]]
        }
        """;

        var store = MakeStore(req => req.Method == HttpMethod.Get
            ? CollectionResponse()
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(queryJson) });

        var results = await store.SearchAsync(new[] { 0.1f }, topK: 5);

        Assert.Empty(results);
    }

    [Fact]
    public async Task Search_ShouldReturnEmpty_WhenQueryRequestFails()
    {
        var store = MakeStore(req => req.Method == HttpMethod.Get
            ? CollectionResponse()
            : new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("boom") });

        var results = await store.SearchAsync(new[] { 0.1f }, topK: 5);

        Assert.Empty(results); // graceful degradation
    }

    [Fact]
    public async Task Search_ShouldReturnEmpty_WhenConnectionThrows()
    {
        var store = MakeStore(_ => throw new HttpRequestException("network down"));

        var results = await store.SearchAsync(new[] { 0.1f }, topK: 5);

        Assert.Empty(results); // connection failure never throws to the caller
    }

    [Fact]
    public async Task Search_ShouldReturnEmpty_WhenVectorIsEmpty()
    {
        var store = MakeStore(_ => CollectionResponse());

        var results = await store.SearchAsync([], topK: 5);

        Assert.Empty(results);
    }

    [Fact]
    public async Task Search_ShouldIncludeMetadataFilter_InQueryRequest()
    {
        string? capturedBody = null;
        var queryJson = """{"ids":[[]],"documents":[[]],"metadatas":[[]],"distances":[[]]}""";

        var store = MakeStore(req =>
        {
            if (req.Method == HttpMethod.Get)
                return CollectionResponse();
            capturedBody = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(queryJson) };
        });

        await store.SearchAsync(new[] { 0.1f }, conceptTag: "Graphs", source: "concept", topK: 3);

        Assert.NotNull(capturedBody);
        Assert.Contains("concept_tag", capturedBody);
        Assert.Contains("Graphs", capturedBody);
        Assert.Contains("\"source\"", capturedBody);
    }

    // ── Test double ───────────────────────────────────────────────

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}
