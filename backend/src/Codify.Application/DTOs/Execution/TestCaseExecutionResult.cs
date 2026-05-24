namespace Codify.Application.DTOs.Execution;

public class TestCaseExecutionResult
{
    public string ActualOutput { get; set; } = string.Empty;
    public string Stderr { get; set; } = string.Empty;
    public int ExecutionTimeMs { get; set; }
    public int MemoryUsedKb { get; set; }
    public bool TimedOut { get; set; }
    public bool CompileError { get; set; }
    public bool RuntimeError { get; set; }
}
