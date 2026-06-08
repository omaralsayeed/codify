using System.ComponentModel.DataAnnotations;

namespace Codify.Application.DTOs.Execution;

/// <summary>
/// A single test case — the input to feed the program and the output we expect.
/// </summary>
public class TestCaseRequest
{
    /// <summary>
    /// What we pipe into stdin (e.g. "5").
    /// Can be empty if the code doesn't call input().
    /// </summary>
    public string Input { get; set; } = string.Empty;

    /// <summary>
    /// What we expect the program to print (e.g. "25").
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "expectedOutput must not be empty.")]
    public string ExpectedOutput { get; set; } = string.Empty;
}
