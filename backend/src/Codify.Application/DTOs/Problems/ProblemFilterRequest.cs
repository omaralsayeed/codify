using Codify.Domain.Enums;

namespace Codify.Application.DTOs.Problems;

public class ProblemFilterRequest
{
    public Difficulty? Difficulty { get; set; }
    public string? Tag { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
