using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Codify.Infrastructure.Search;

/// <summary>
/// Searches the knowledge base (DSA concept docs) for the Tutor Agent.
/// The interface (IKnowledgeBaseSearchService) is pgvector-ready; this
/// implementation searches ConceptTag descriptions by keyword/tag match
/// until a pgvector pipeline is wired in.
/// </summary>
public class KnowledgeBaseSearchService(IConceptTagRepository tagRepo) : IKnowledgeBaseSearchService
{
    private readonly IConceptTagRepository _tagRepo = tagRepo;

    public async Task<List<KnowledgeBaseResult>> SearchAsync(
        string query, string? conceptTag = null, int topK = 5)
    {
        var tags = await _tagRepo.GetAllAsync();
        var queryLower = (query ?? string.Empty).ToLowerInvariant();
        var results = new List<KnowledgeBaseResult>();

        foreach (var tag in tags)
        {
            // Filter by concept tag if specified.
            if (!string.IsNullOrWhiteSpace(conceptTag)
                && !tag.Name.Equals(conceptTag, StringComparison.OrdinalIgnoreCase))
                continue;

            var nameLower = tag.Name.ToLowerInvariant();
            var descLower = tag.Description.ToLowerInvariant();

            // Simple relevance scoring: keyword overlap.
            var score = 0.0;
            foreach (var word in queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (nameLower.Contains(word)) score += 2.0;
                if (descLower.Contains(word)) score += 1.0;
            }
            if (score <= 0) continue;

            results.Add(new KnowledgeBaseResult
            {
                Content = $"{tag.Name}: {tag.Description}",
                ConceptTag = tag.Name,
                Relevance = score
            });
        }

        return results
            .OrderByDescending(r => r.Relevance)
            .Take(topK)
            .ToList();
    }
}
