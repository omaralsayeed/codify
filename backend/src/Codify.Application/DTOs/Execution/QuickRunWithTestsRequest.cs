using System.ComponentModel.DataAnnotations;

namespace Codify.Application.DTOs.Execution;

/// <summary>
/// Request body for the test-cases execution endpoint.
/// </summary>
public class QuickRunWithTestsRequest
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "language must not be empty.")]
    public string Language { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "code must not be empty.")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// One or more test cases to run the code against.
    /// </summary>
    [Required, MinLength(1, ErrorMessage = "At least one test case is required.")]
    public List<TestCaseRequest> TestCases { get; set; } = [];
}
