using System.Diagnostics;
using Codify.Application.DTOs.Execution;
using Codify.Application.Execution;
using Codify.Application.Interfaces;

namespace Codify.Application.Services;

public class QuickRunWithTestsService : IQuickRunWithTestsService
{
    public async Task<QuickRunWithTestsResponse> RunAsync(QuickRunWithTestsRequest request)
    {
        var config = LanguageConfig.Get(request.Language);
        if (config is null)
        {
            var unsupported = request.TestCases.Select(tc => new TestCaseResult
            {
                Input    = tc.Input,
                Expected = tc.ExpectedOutput,
                Actual   = string.Empty,
                Passed   = false,
                Status   = "error",
                Stderr   = $"Language '{request.Language}' is not supported. Supported: {LanguageConfig.SupportedLanguages}."
            }).ToList();

            return new QuickRunWithTestsResponse { Results = unsupported };
        }

        var tempFolder = Path.Combine(Path.GetTempPath(), $"codify_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempFolder, config.FileName), request.Code);

            if (config.ProjectFileName is not null && config.ProjectFileContent is not null)
                await File.WriteAllTextAsync(Path.Combine(tempFolder, config.ProjectFileName), config.ProjectFileContent);

            var results = new List<TestCaseResult>();

            foreach (var testCase in request.TestCases)
            {
                var result = await RunSingleTestCase(tempFolder, config, testCase);
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

    private static async Task<TestCaseResult> RunSingleTestCase(
        string tempFolder,
        LanguageConfig config,
        TestCaseRequest testCase)
    {
        var dockerArgs = string.Join(" ",
            "run", "--rm", "-i", "--network none", "--memory=128m",
            $"-v \"{tempFolder}\":/app",
            config.DockerImage,
            config.RunCommand);

        var stopwatch = Stopwatch.StartNew();

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName               = "docker",
            Arguments              = dockerArgs,
            RedirectStandardInput  = true,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true
        };

        process.Start();

        await process.StandardInput.WriteLineAsync(testCase.Input);
        process.StandardInput.Close();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        var finished = process.WaitForExit(config.TimeoutMs);
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
                Stderr          = $"Execution exceeded the {config.TimeoutMs / 1000}s time limit.",
                ExecutionTimeMs = config.TimeoutMs
            };
        }

        var stdout = (await stdoutTask).Trim();
        var stderr = (await stderrTask).Trim();

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

        var passed = string.Equals(stdout, testCase.ExpectedOutput.Trim(), StringComparison.Ordinal);

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
