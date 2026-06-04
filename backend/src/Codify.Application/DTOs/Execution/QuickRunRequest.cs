using System.ComponentModel.DataAnnotations;

namespace Codify.Application.DTOs.Execution;

/// <summary>
/// Request body for the Day-2 skeleton execution endpoint.
/// Accepts a free-form language string and source code.
/// No problem context is required — this is a stateless quick-run entry point.
/// </summary>
public class QuickRunRequest
{
    /// <summary>
    /// Programming language identifier (e.g. "python", "javascript", "csharp").
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "language must not be empty.")]
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Source code to execute.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "code must not be empty.")]
    public string Code { get; set; } = string.Empty;
}
