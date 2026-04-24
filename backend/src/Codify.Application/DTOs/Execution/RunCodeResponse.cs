namespace Codify.Application.DTOs.Execution;

public class RunCodeResponse
{
    public string Stdout { get; set; } = string.Empty;
    public string Stderr { get; set; } = string.Empty;
    public int ExecutionTimeMs { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<SampleTestResult> TestResults { get; set; } = [];
}

public class SampleTestResult
{
    public string Input { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
    public string ActualOutput { get; set; } = string.Empty;
    public bool Passed { get; set; }
}
