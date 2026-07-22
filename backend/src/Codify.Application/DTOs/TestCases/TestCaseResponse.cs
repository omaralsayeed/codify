using Codify.Domain.Enums;

namespace Codify.Application.DTOs.TestCases;

public class TestCaseResponse
{
    public Guid Id { get; set; }
    public Guid ProblemId { get; set; }
    public string Input { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
    public bool IsSample { get; set; }
    public TestCaseVisibility VisibilityMode { get; set; }
    public int OrderIndex { get; set; }
    public DateTime CreatedAt { get; set; }
}
