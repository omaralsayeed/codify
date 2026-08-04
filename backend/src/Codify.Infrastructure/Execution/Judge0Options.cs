namespace Codify.Infrastructure.Execution;

/// <summary>
/// Options for the Judge0 sandboxed execution service.
/// Bound from the "Judge0" configuration section.
/// </summary>
public class Judge0Options
{
    public const string SectionName = "Judge0";

    /// <summary>Base URL of the Judge0 instance (e.g. http://localhost:2358).</summary>
    public string BaseUrl { get; set; } = "http://localhost:2358";

    /// <summary>Optional Judge0 X-Auth-Token for authenticated instances.</summary>
    public string AuthToken { get; set; } = string.Empty;

    /// <summary>HTTP timeout for a single submission (ms).</summary>
    public int TimeoutMs { get; set; } = 30000;
}

/// <summary>Judge0 language IDs (from the ExecutionEngine README).</summary>
public static class Judge0LanguageIds
{
    public const int Python3 = 71;
    public const int CSharpNet = 2225;
}
