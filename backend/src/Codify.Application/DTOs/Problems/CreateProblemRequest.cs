using System.ComponentModel.DataAnnotations;
using Codify.Domain.Enums;

namespace Codify.Application.DTOs.Problems;

public class CreateProblemRequest
{
    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Statement { get; set; } = string.Empty;

    [Required]
    public Difficulty Difficulty { get; set; }

    public string Constraints { get; set; } = string.Empty;

    public List<string> LanguageSupport { get; set; } = ["Python", "CSharp"];

    public List<Guid> TagIds { get; set; } = [];

    public List<CreateTestCaseRequest> TestCases { get; set; } = [];

    [Range(100, 30000)]
    public int TimeLimitMs { get; set; } = 2000;

    [Range(16, 1024)]
    public int MemoryLimitMb { get; set; } = 256;
}

public class CreateTestCaseRequest
{
    [Required]
    public string InputData { get; set; } = string.Empty;

    [Required]
    public string ExpectedOutput { get; set; } = string.Empty;

    public bool IsSample { get; set; }

    public TestCaseVisibility VisibilityMode { get; set; } = TestCaseVisibility.Hidden;
}
