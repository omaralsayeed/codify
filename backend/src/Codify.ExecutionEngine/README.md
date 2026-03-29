# Codify.ExecutionEngine

> **Sandboxed code execution.** Runs student code in Docker containers, captures output, evaluates against test cases.

## ⚠️ This is the highest-risk component

Read this entire file before writing any code here.

## What Goes Here

- `DockerRunner.cs` — Spins up containers, runs code, captures stdout/stderr
- `TestCaseEvaluator.cs` — Compares actual output to expected output
- `ExecutionService.cs` — Orchestrates the execution flow, implements `IExecutionService`
- `LanguageConfig/` — Per-language Docker image, compile command, run command config

## Execution Flow

```
1. Receive: code (string), language, test cases
2. Write code to a temp file
3. Pull/verify Docker image for the language
4. Start container with:
   - Volume mount: temp file → /code/solution.py (or .cs)
   - Network disabled
   - Memory limit: 128MB
   - CPU limit: 0.5 cores
   - Timeout: 5 seconds
5. Run the code
6. Capture stdout and stderr
7. Compare stdout to expected output (exact string match, trimmed)
8. Destroy container
9. Return result
```

## Language Configuration

```csharp
// LanguageConfig/PythonConfig.cs
public static class PythonConfig
{
    public const string DockerImage = "python:3.12-slim";
    public const string RunCommand = "python /code/solution.py";
    public const string FileExtension = ".py";
    public const int TimeLimitSeconds = 5;
    public const int MemoryLimitMb = 128;
}

// LanguageConfig/CSharpConfig.cs
public static class CSharpConfig
{
    public const string DockerImage = "mcr.microsoft.com/dotnet/sdk:8.0";
    public const string CompileCommand = "dotnet build /code";
    public const string RunCommand = "dotnet run --project /code";
    public const string FileExtension = ".cs";
    public const int TimeLimitSeconds = 10; // Longer due to JIT
    public const int MemoryLimitMb = 256;
}
```

## Security Rules (Non-Negotiable)

- Network access is **always disabled** in execution containers
- Never mount the host filesystem except for the single code file
- Container is **always destroyed** after execution — success or failure
- Code never runs on the host directly — always in Docker
- Log all execution attempts with userId and submissionId (for audit)

## Fallback: Judge0

If Docker proves unstable or unsafe to configure in time, Judge0 is the fallback.

Judge0 is an open-source online judge. It provides an HTTP API for code execution:

```
POST https://judge0-instance/submissions
{
  "source_code": "...",
  "language_id": 71,  // Python 3
  "stdin": "input here",
  "expected_output": "expected here"
}
```

Judge0 language IDs:
- Python 3: `71`
- C# (Mono): `51`

To switch to Judge0: implement `IExecutionService` using the Judge0 HTTP API instead of Docker.

## Output Comparison

```csharp
// Exact match after trimming whitespace
bool passed = actualOutput.Trim() == expectedOutput.Trim();
```

For floating point problems (not in MVP scope): add tolerance comparison.

## See Also

- [ARCHITECTURE.md](../../../../docs/architecture/ARCHITECTURE.md) for how execution fits in the system
- [API_SPEC.md](../../../../docs/api/API_SPEC.md) for the POST /execution/run endpoint spec
