namespace Codify.Application.Interfaces;

/// <summary>
/// Searches the knowledge base (DSA concept docs) for the Tutor Agent.
/// The interface is pgvector-ready; the current implementation searches
/// ConceptTag descriptions by keyword/tag match.
/// </summary>
public interface IKnowledgeBaseSearchService
{
    Task<List<KnowledgeBaseResult>> SearchAsync(string query, string? conceptTag = null, int topK = 5);
}

public class KnowledgeBaseResult
{
    public string Content { get; set; } = string.Empty;
    public string ConceptTag { get; set; } = string.Empty;
    public double Relevance { get; set; }
}
