using System.Net.Http.Json;
using System.Text.Json;
using Codify.Application.DTOs.Execution;
using Codify.Application.Interfaces;
using Codify.Domain.Enums;
using Codify.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Codify.Infrastructure.Execution;

/// <summary>
/// Sandboxed execution service backed by Judge0. Replaces the stub
/// ExecutionService with real isolated execution. Implements IExecutionService
/// so the API contract (RunCodeResponse) is unchanged.
/// </summary>
public class Judge0ExecutionService(
    HttpClient httpClient,
    IProblemRepository problemRepo,
    IOptions<Judge0Options> options,
    ILogger<Judge0ExecutionService> logger) : IExecutionService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly IProblemRepository _problemRepo = problemRepo;
    private readonly Judge0Options _options = options.Value;
    private readonly ILogger<Judge0ExecutionService> _logger = logger;

    public async Task<RunCodeResponse> RunAsync(RunCodeRequest request)
    {
        var problem = await _problemRepo.GetByIdWithTestCasesAsync(request.ProblemId)
            ?? throw new NotFoundException($"Problem {request.ProblemId} not found.");

        var sampleCases = problem.TestCases
            .Where(tc => tc.IsSample)
            .OrderBy(tc => tc.OrderIndex)
            .ToList();

        if (sampleCases.Count == 0)
        {
            return new RunCodeResponse
            {
                Status = "NoSampleCases",
                Stdout = string.Empty,
                Stderr = "No sample test cases available for this problem.",
                ExecutionTimeMs = 0,
                TestResults = []
            };
        }

        var langId = request.Language switch
        {
            SubmissionLanguage.Python => Judge0LanguageIds.Python3,
            SubmissionLanguage.CSharp => Judge0LanguageIds.CSharpNet,
            _ => throw new ValidationException($"Unsupported language: {request.Language}")
        };

        var timeLimitS = Math.Max(1, problem.TimeLimitMs / 1000);
        var memoryLimitKb = problem.MemoryLimitMb * 1024;

        var testResults = new List<SampleTestResult>();
        var allStdout = new System.Text.StringBuilder();
        var allStderr = new System.Text.StringBuilder();
        var maxTimeMs = 0;
        var anyFailed = false;

        foreach (var tc in sampleCases)
        {
            var result = await SubmitAndWaitAsync(
                request.Code, langId, tc.InputData, tc.ExpectedOutput, timeLimitS, memoryLimitKb);

            if (result.ExecutionTimeMs > maxTimeMs)
                maxTimeMs = result.ExecutionTimeMs;

            testResults.Add(new SampleTestResult
            {
                Input = tc.InputData,
                ExpectedOutput = tc.ExpectedOutput,
                ActualOutput = result.Stdout,
                Passed = result.Passed
            });

            if (!string.IsNullOrEmpty(result.Stdout))
                allStdout.AppendLine(result.Stdout);
            if (!string.IsNullOrEmpty(result.Stderr))
                allStderr.AppendLine(result.Stderr);
            if (!result.Passed)
                anyFailed = true;
        }

        return new RunCodeResponse
        {
            Status = anyFailed ? "Failed" : "Accepted",
            Stdout = allStdout.ToString(),
            Stderr = allStderr.ToString(),
            ExecutionTimeMs = maxTimeMs,
            TestResults = testResults
        };
    }


    private async Task<(string Stdout, string Stderr, bool Passed, int ExecutionTimeMs)> SubmitAndWaitAsync(
        string code, int langId, string stdin, string expected, int timeLimitS, int memoryLimitKb)
    {
        var payload = new
        {
            language_id = langId,
            source_code = ToBase64(code),
            stdin = ToBase64(stdin),
            expected_output = ToBase64(expected),
            cpu_time_limit = timeLimitS,
            memory_limit = memoryLimitKb,
            enable_network = false
        };

        try
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/submissions?base64_encoded=true&wait=true")
            {
                Content = JsonContent.Create(payload)
            };
            if (!string.IsNullOrEmpty(_options.AuthToken))
                requestMessage.Headers.Add("X-Auth-Token", _options.AuthToken);

            var response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var submission = JsonSerializer.Deserialize<Judge0Submission>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (submission is null)
                return (string.Empty, "Judge0 returned no data.", false, 0);

            var stdout = FromBase64(submission.Stdout);
            var stderr = FromBase64(submission.Stderr);
            var compileOutput = FromBase64(submission.CompileOutput);
            var statusId = submission.Status?.Id ?? 0;
            var timeMs = (int)((submission.Time ?? 0) * 1000);

            var passed = statusId == 3 && Normalize(stdout) == Normalize(expected);

            if (statusId == 6 && !string.IsNullOrEmpty(compileOutput))
                stderr = string.IsNullOrEmpty(stderr) ? compileOutput : stderr + "\n" + compileOutput;

            return (stdout, stderr, passed, timeMs);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Judge0 submission failed.");
            return (string.Empty, $"Execution service unavailable: {ex.Message}", false, 0);
        }
    }

    private static string ToBase64(string value) =>
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value ?? string.Empty));

    private static string FromBase64(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        try { return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(value)); }
        catch { return value; }
    }

    private static string Normalize(string text) =>
        string.Join("\n", (text ?? string.Empty).Split('\n').Select(l => l.TrimEnd())).Trim();

    private sealed class Judge0Submission
    {
        public string? Stdout { get; set; }
        public string? Stderr { get; set; }
        public string? CompileOutput { get; set; }
        public double? Time { get; set; }
        public Judge0Status? Status { get; set; }
    }

    private sealed class Judge0Status
    {
        public int Id { get; set; }
        public string? Description { get; set; }
    }
}
