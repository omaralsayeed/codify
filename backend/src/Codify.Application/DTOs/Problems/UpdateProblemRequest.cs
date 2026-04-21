using Codify.Domain.Enums;

namespace Codify.Application.DTOs.Problems;

public class UpdateProblemRequest
{
    public string? Title { get; set; }
    public string? Statement { get; set; }
    public Difficulty? Difficulty { get; set; }
    public string? Constraints { get; set; }
    public List<string>? LanguageSupport { get; set; }
    public List<Guid>? TagIds { get; set; }
}
