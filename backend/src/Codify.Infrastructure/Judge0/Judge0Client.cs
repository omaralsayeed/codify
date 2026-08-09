using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Codify.Application.DTOs.Execution;
using Codify.Application.Execution;
using Codify.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Codify.Infrastructure.Judge0;

/// <summary>
/// Real Judge0 HTTP client. Submits code, then polls GET /submissions/{token}
/// until Judge0 reports a terminal status or the poll budget runs out.
/// The HttpClient (base address + auth headers) is configured once at DI
/// registration time via AddHttpClient — see DependencyInjection.cs.
/// </summary>
public class Judge0Client(
    HttpClient httpClient,
    IOptions<Judge0Options> options,
    ILogger<Judge0Client> logger) : IJudge0Client
{
    private readonly Judge0Options _options = options.Value;

    public async Task<Judge0SubmissionResult> ExecuteAsync(
        Judge0SubmissionRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await CreateSubmissionAsync(request, cancellationToken);
        return await PollUntilCompleteAsync(token, cancellationToken);
    }

    // ── Private ───────────────────────────────────────────────────

    private async Task<string> CreateSubmissionAsync(
        Judge0SubmissionRequest request, CancellationToken cancellationToken)
    {
        var payload = new Judge0CreatePayload(
            request.SourceCode,
            request.LanguageId,
            request.Stdin,
            request.CpuTimeLimitSeconds,
            request.MemoryLimitKb);

        using var response = await httpClient.PostAsJsonAsync(
            "submissions?base64_encoded=false&wait=false", payload, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError(
                "Judge0 submission creation failed. Status={StatusCode} Body={Body}",
                (int)response.StatusCode, body);
            throw new InvalidOperationException(
                $"Judge0 rejected the submission (HTTP {(int)response.StatusCode}).");
        }

        var created = await response.Content.ReadFromJsonAsync<Judge0CreatedResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Judge0 returned an empty submission response.");

        return created.Token;
    }

    private async Task<Judge0SubmissionResult> PollUntilCompleteAsync(
        string token, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= _options.MaxPollAttempts; attempt++)
        {
            var result = await FetchSubmissionAsync(token, cancellationToken);

            if (Judge0Status.IsTerminal(result.StatusId))
                return result;

            logger.LogDebug(
                "Judge0 submission {Token} still {Status} (attempt {Attempt}/{Max}).",
                token, result.StatusDescription, attempt, _options.MaxPollAttempts);

            await Task.Delay(_options.PollIntervalMs, cancellationToken);
        }

        logger.LogWarning(
            "Judge0 submission {Token} did not reach a terminal status within {MaxAttempts} polling attempts.",
            token, _options.MaxPollAttempts);

        return new Judge0SubmissionResult
        {
            StatusId = Judge0Status.Processing,
            StatusDescription = "Polling budget exhausted",
            PollTimedOut = true
        };
    }

    private async Task<Judge0SubmissionResult> FetchSubmissionAsync(
        string token, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"submissions/{token}?base64_encoded=false" +
            "&fields=stdout,stderr,compile_output,message,status,time,memory",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<Judge0StatusResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Judge0 returned an empty status response.");

        return new Judge0SubmissionResult
        {
            StatusId = body.Status?.Id ?? 0,
            StatusDescription = body.Status?.Description ?? "Unknown",
            Stdout = body.Stdout,
            Stderr = body.Stderr,
            CompileOutput = body.CompileOutput,
            Message = body.Message,
            TimeSeconds = body.Time,
            MemoryKb = body.Memory
        };
    }

    // ── Judge0 wire DTOs (snake_case JSON) ───────────────────────────

    private record Judge0CreatePayload(
        [property: JsonPropertyName("source_code")] string SourceCode,
        [property: JsonPropertyName("language_id")] int LanguageId,
        [property: JsonPropertyName("stdin")] string? Stdin,
        [property: JsonPropertyName("cpu_time_limit")] double CpuTimeLimitSeconds,
        [property: JsonPropertyName("memory_limit")] int MemoryLimitKb);

    private record Judge0CreatedResponse(
        [property: JsonPropertyName("token")] string Token);

    private record Judge0StatusResponse(
        [property: JsonPropertyName("stdout")] string? Stdout,
        [property: JsonPropertyName("stderr")] string? Stderr,
        [property: JsonPropertyName("compile_output")] string? CompileOutput,
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("time")] string? Time,
        [property: JsonPropertyName("memory")] int? Memory,
        [property: JsonPropertyName("status")] Judge0StatusPayload? Status);

    private record Judge0StatusPayload(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("description")] string Description);
}
