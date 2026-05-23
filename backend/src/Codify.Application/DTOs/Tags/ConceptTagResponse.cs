namespace Codify.Application.DTOs.Tags;

// This is what the API returns to the client for any tag operation.
// Slug is the URL-safe version of the name (e.g. "Dynamic Programming" -> "dynamic-programming").
public class ConceptTagResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
