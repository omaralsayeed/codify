using Codify.Domain.Enums;

namespace Codify.Application.DTOs.Problems;

public class ProblemSummaryResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Difficulty Difficulty { get; set; }
    public List<string> Tags { get; set; } = [];
    public bool IsActive { get; set; }
}
