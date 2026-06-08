using System.Diagnostics;
using Codify.Application.DTOs.Execution;
using Codify.Application.Interfaces;

namespace Codify.Application.Services;

/// <summary>
/// Day 4 implementation: runs code against multiple test cases and compares output.
///
/// For every test case:
///   1. Write code to a temp file
///   2. Run Docker and pipe the test input into stdin
///   3. Capture stdout
///   4. Compare actual output to expected output (trimmed)
///   5. Return pass/fail
/// </summary>
public class QuickRunWithTestsService : IQuickRunWithTestsService
{
    private const int TimeoutMs = 5000;

    public async Task<QuickRunWithTestsResponse> RunAsync(QuickRunWithTestsRequest request)
    {
        if (!request.Language.Equals("python", StringComparison.OrdinalIgnoreCase))
        {
            // Return all cases as errored — language not supported yet
            var unsupported = request.TestCases.Select(tc => new TestCaseResult
            {
                Input    = tc.Input,
                Expected = tc.ExpectedOutput,
                Actual   = string.Empty,
                Passed   = false,
                Status   = "error",
                Stderr   = $"Language '{request.Language}' is not supported yet."
            }).ToList();

            return new QuickRunWithTestsResponse { Results = unsupported };
        }

        // We create ONE temp folder per request and reuse it for all test cases.
        // The code file is written once — only the input changes per test case.
        var tempFolder = Path.Combine(Path.GetTempPath(), $"codify_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            // Write the code file once — same code runs for every test case
            var codeFilePath = Path.Combine(tempFolder, "main.py");
            await File.WriteAllTextAsync(codeFilePath, request.Code);

            var results = new List<TestCaseResult>();

            foreach (var testCase in request.TestCases)
            {
                var result = await RunSingleTestCase(tempFolder, testCase);
                results.Add(result);
            }

            return new QuickRunWithTestsResponse { Results = results };
        }
        finally
        {
            if (Directory.Exists(tempFolder))
                Directory.Delete(tempFolder, recursive: true);
        }
    }

    private async Task<TestCaseResult> RunSingleTestCase(string tempFolder, TestCaseRequest testCase)
    {
        // --- How input injection works ---
        //
        // Python's input() reads from stdin.
        // Docker's -i flag keeps stdin open so we can write to it.
        //
        // We write the test input directly to process.StandardInput
        // after the process starts — Docker forwards it into the container's stdin.
        //
        var dockerArgs = string.Join(" ",
            "run",
            "--rm",
            "-i",              // keep stdin open so we can pipe input
            "--network none",
            "--memory=128m",
            $"-v \"{tempFolder}\":/app",
            "python:3.11",
            "python /app/main.py"
        );

        var stopwatch = Stopwatch.StartNew();

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName               = "docker",
            Arguments              = dockerArgs,
            RedirectStandardInput  = true,   // so we can write the test input
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true
        };

        process.Start();

        // Write the test input then close stdin.
        // Closing stdin signals to the program that there is no more input —
        // same as pressing Ctrl+D in a terminal.
        await process.StandardInput.WriteLineAsync(testCase.Input);
        process.StandardInput.Close();

        // Read stdout and stderr in parallel to avoid deadlock
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        var finished = process.WaitForExit(TimeoutMs);
        stopwatch.Stop();

        if (!finished)
        {
            process.Kill(entireProcessTree: true);

            return new TestCaseResult
            {
                Input           = testCase.Input,
                Expected        = testCase.ExpectedOutput,
                Actual          = string.Empty,
                Passed          = false,
                Status          = "timeout",
                Stderr          = $"Execution exceeded the {TimeoutMs / 1000}s time limit.",
                ExecutionTimeMs = TimeoutMs
            };
        }

        var stdout = (await stdoutTask).Trim();
        var stderr = (await stderrTask).Trim();

        // Exit code 137 = container killed by kernel (OOM) due to memory limit exceeded.
        if (process.ExitCode == 137)
        {
            return new TestCaseResult
            {
                Input           = testCase.Input,
                Expected        = testCase.ExpectedOutput.Trim(),
                Actual          = string.Empty,
                Passed          = false,
                Status          = "memory_limit_exceeded",
                Stderr          = "Process was killed: memory limit exceeded (128MB).",
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }

        // Output comparison
        //
        // We trim both sides before comparing.
        // This handles trailing newlines that Python's print() always adds.
        // "25\n" and "25" are treated as equal — same as real online judges.
        //
        var passed = string.Equals(stdout, testCase.ExpectedOutput.Trim(),
                                   StringComparison.Ordinal);

        return new TestCaseResult
        {
            Input           = testCase.Input,
            Expected        = testCase.ExpectedOutput.Trim(),
            Actual          = stdout,
            Passed          = passed,
            Status          = string.IsNullOrWhiteSpace(stderr) ? "success" : "error",
            Stderr          = stderr,
            ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
        };
    }
}
