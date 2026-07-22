using System.Diagnostics;
using Codify.Application.DTOs.Execution;
using Codify.Application.Execution;
using Codify.Application.Interfaces;

namespace Codify.Application.Services;

public class QuickRunService : IQuickRunService
{
    public async Task<QuickRunResponse> RunAsync(QuickRunRequest request)
    {
        var config = LanguageConfig.Get(request.Language);
        if (config is null)
        {
            return new QuickRunResponse
            {
                Status          = "error",
                Stderr          = $"Language '{request.Language}' is not supported. Supported: {LanguageConfig.SupportedLanguages}.",
                Stdout          = string.Empty,
                ExecutionTimeMs = 0
            };
        }

        var tempFolder = Path.Combine(Path.GetTempPath(), $"codify_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempFolder, config.FileName), request.Code);

            if (config.ProjectFileName is not null && config.ProjectFileContent is not null)
                await File.WriteAllTextAsync(Path.Combine(tempFolder, config.ProjectFileName), config.ProjectFileContent);

            var dockerArgs = string.Join(" ",
                "run", "--rm", "--network none", "--memory=128m",
                $"-v \"{tempFolder}\":/app",
                config.DockerImage,
                config.RunCommand);

            var stopwatch = Stopwatch.StartNew();

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName               = "docker",
                Arguments              = dockerArgs,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };

            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            var finished = process.WaitForExit(config.TimeoutMs);
            stopwatch.Stop();

            if (!finished)
            {
                process.Kill(entireProcessTree: true);
                return new QuickRunResponse
                {
                    Status          = "timeout",
                    Stdout          = string.Empty,
                    Stderr          = $"Execution exceeded the {config.TimeoutMs / 1000}s time limit.",
                    ExecutionTimeMs = config.TimeoutMs
                };
            }

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            if (process.ExitCode == 137)
            {
                return new QuickRunResponse
                {
                    Status          = "memory_limit_exceeded",
                    Stdout          = string.Empty,
                    Stderr          = "Process was killed: memory limit exceeded (128MB).",
                    ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
                };
            }

            return new QuickRunResponse
            {
                Status          = string.IsNullOrWhiteSpace(stderr) ? "success" : "error",
                Stdout          = stdout.Trim(),
                Stderr          = stderr.Trim(),
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
        finally
        {
            if (Directory.Exists(tempFolder))
                Directory.Delete(tempFolder, recursive: true);
        }
    }
}
