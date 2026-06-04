using System.Diagnostics;
using Codify.Application.DTOs.Execution;
using Codify.Application.Interfaces;

namespace Codify.Application.Services;

/// <summary>
/// Day 3 implementation: runs code inside a Docker container and captures the output.
///
/// Flow:
///   1. Create a unique temp folder
///   2. Write the code to a file inside it
///   3. Run docker with a volume mount pointing to that folder
///   4. Capture stdout + stderr
///   5. Clean up the temp folder
///   6. Return the result
/// </summary>
public class QuickRunService : IQuickRunService
{
    // How many milliseconds we wait before killing the container.
    // Protects against infinite loops (e.g.  while True: pass)
    private const int TimeoutMs = 5000;

    public async Task<QuickRunResponse> RunAsync(QuickRunRequest request)
    {
        // Only Python is supported in Day 3.
        // Day 6 will add C# by switching the Docker image and file name.
        if (!request.Language.Equals("python", StringComparison.OrdinalIgnoreCase))
        {
            return new QuickRunResponse
            {
                Status = "error",
                Stderr = $"Language '{request.Language}' is not supported yet.",
                Stdout = string.Empty,
                ExecutionTimeMs = 0
            };
        }

        // --- Step 1: create a unique temp folder ---
        // We use a GUID so two requests never share the same folder.
        var tempFolder = Path.Combine(Path.GetTempPath(), $"codify_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            // --- Step 2: write the code to main.py ---
            var codeFilePath = Path.Combine(tempFolder, "main.py");
            await File.WriteAllTextAsync(codeFilePath, request.Code);

            // --- Step 3: build the docker command ---
            //
            // Breakdown of every flag:
            //   --rm          → delete the container automatically after it exits
            //   --network none → no internet access (security)
            //   --memory=128m → container can't use more than 128 MB RAM
            //   -v <host>:/app → mount our temp folder inside the container as /app
            //   python:3.11   → the Docker image we want to run
            //   python /app/main.py → the command to run inside the container
            //
            var dockerArgs = string.Join(" ",
                "run",
                "--rm",
                "--network none",
                "--memory=128m",
                $"-v \"{tempFolder}\":/app",
                "python:3.11",
                "python /app/main.py"
            );

            // --- Step 4: start the process and measure time ---
            var stopwatch = Stopwatch.StartNew();

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName               = "docker",
                Arguments              = dockerArgs,
                RedirectStandardOutput = true,   // capture what the code prints
                RedirectStandardError  = true,   // capture error messages
                UseShellExecute        = false,  // required for redirection to work
                CreateNoWindow         = true    // don't open a terminal window
            };

            process.Start();

            // Read output WHILE the process is still running.
            // We do both reads at the same time (async) to avoid a deadlock.
            // Deadlock happens when: stdout buffer is full so process waits,
            // but we're waiting on stderr → everyone is stuck forever.
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            // --- Step 5: wait for the process, but not forever ---
            // WaitForExit(TimeoutMs) returns true if process finished, false if timeout.
            var finished = process.WaitForExit(TimeoutMs);

            stopwatch.Stop();

            if (!finished)
            {
                // The code is probably stuck in an infinite loop.
                // Kill the container so it doesn't run forever.
                process.Kill(entireProcessTree: true);

                return new QuickRunResponse
                {
                    Status         = "timeout",
                    Stdout         = string.Empty,
                    Stderr         = $"Execution exceeded the {TimeoutMs / 1000}s time limit.",
                    ExecutionTimeMs = TimeoutMs
                };
            }

            // Collect the output now that the process is done
            var stdout = await stdoutTask;
            var stderr = await stderrTask;

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
            // --- Step 6: always clean up the temp folder ---
            // 'finally' runs even if an exception is thrown above.
            if (Directory.Exists(tempFolder))
                Directory.Delete(tempFolder, recursive: true);
        }
    }
}
