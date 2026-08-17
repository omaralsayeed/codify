using System.ComponentModel.DataAnnotations;
using Codify.Domain.Enums;

namespace Codify.Application.DTOs.Problems;

public class CreateProblemRequest
{
    /// <summary>Required. Min 3 chars. Must be unique — returns 409 if duplicate.</summary>
    [Required, MinLength(3), MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Required. Min 50 chars.</summary>
    [Required, MinLength(50)]
    public string Statement { get; set; } = string.Empty;

    /// <summary>Required. 0 = Easy, 1 = Medium, 2 = Hard.</summary>
    [Required]
    public Difficulty Difficulty { get; set; }

    public string Constraints { get; set; } = string.Empty;

    public List<string> LanguageSupport { get; set; } = ["Python", "CSharp"];

    /// <summary>
    /// Tag names as strings (e.g. ["Arrays", "Hash Map"]).
    /// The service resolves names to ConceptTag entities — creates new tags if they don't exist.
    /// At least 1 required.
    /// </summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// Sample test cases shown to students. At least 1 required.
    /// Each must have non-empty Input and ExpectedOutput.
    /// </summary>
    public List<SampleTestCaseInput> SampleTestCases { get; set; } = [];

    [Range(100, 30000)]
    public int TimeLimitMs { get; set; } = 2000;

    [Range(16, 1024)]
    public int MemoryLimitMb { get; set; } = 256;

    /// <summary>Whether the problem is visible to students immediately. Defaults to true.</summary>
    public bool IsActive { get; set; } = true;
}
