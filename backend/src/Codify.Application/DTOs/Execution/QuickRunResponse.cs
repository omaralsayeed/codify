namespace Codify.Application.DTOs.Execution;

/// <summary>
/// Response for the quick-run execution endpoint.
/// Contains the raw output from the Docker container.
/// </summary>
public class QuickRunResponse
{
    /// <summary>
    /// Everything the code printed to standard output.
    /// </summary>
    public string Stdout { get; set; } = string.Empty;

    /// <summary>
    /// Error messages if the code crashed or had a syntax error.
    /// </summary>
    public string Stderr { get; set; } = string.Empty;

    /// <summary>
    /// How long the execution took in milliseconds.
    /// </summary>
    public int ExecutionTimeMs { get; set; }

    /// <summary>
    /// Human-readable status: "success", "error", or "timeout"
    /// </summary>
    public string Status { get; set; } = string.Empty;
}
