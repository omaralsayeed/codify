using Codify.Domain.Enums;

namespace Codify.Application.DTOs.Problems;

public class ProblemDetailResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Statement { get; set; } = string.Empty;
    public Difficulty Difficulty { get; set; }
    public string Constraints { get; set; } = string.Empty;
    public List<string> LanguageSupport { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public List<SampleTestCaseResponse> SampleTestCases { get; set; } = [];
    public bool IsActive { get; set; }
}

public class SampleTestCaseResponse
{
    public string Input { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
}
