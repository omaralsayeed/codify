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
        logger.LogInformation("🎯 [JUDGE0-CLIENT] Starting execution: LanguageId={LanguageId}, CodeLength={CodeLength}, InputLength={InputLength}, CpuLimit={CpuLimit}s, MemoryLimit={MemoryLimit}KB, BaseAddress={BaseAddress}", 
            request.LanguageId, request.SourceCode.Length, request.Stdin?.Length ?? 0, 
            request.CpuTimeLimitSeconds, request.MemoryLimitKb, httpClient.BaseAddress);
        
        try
        {
            var token = await CreateSubmissionAsync(request, cancellationToken);
            logger.LogInformation("✅ [JUDGE0-CLIENT] Submission created successfully, Token={Token}", token);
            
            var result = await PollUntilCompleteAsync(token, cancellationToken);
            logger.LogInformation("✅ [JUDGE0-CLIENT] Execution completed: Token={Token}, StatusId={StatusId}, Status={Status}, Time={Time}s, Memory={Memory}KB, PollTimedOut={PollTimedOut}", 
                token, result.StatusId, result.StatusDescription, result.TimeSeconds, result.MemoryKb, result.PollTimedOut);
            
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "🔴 [JUDGE0-CLIENT] Execution FAILED! ErrorType={ErrorType}, Message={Message}, BaseAddress={BaseAddress}, InnerException={InnerException}", 
                ex.GetType().Name, ex.Message, httpClient.BaseAddress, ex.InnerException?.Message ?? "None");
            throw;
        }
    }

    // ── Private ───────────────────────────────────────────────────

    private async Task<string> CreateSubmissionAsync(
        Judge0SubmissionRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("📤 [JUDGE0-CLIENT] Sending submission to Judge0: URL={Url}, LanguageId={LanguageId}", 
            $"{httpClient.BaseAddress}submissions?base64_encoded=false&wait=false", request.LanguageId);
        
        var payload = new Judge0CreatePayload(
            request.SourceCode,
            request.LanguageId,
            request.Stdin,
            request.CpuTimeLimitSeconds,
            request.MemoryLimitKb);

        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                "submissions?base64_encoded=false&wait=false", payload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError(
                    "🔴 [JUDGE0-CLIENT] Submission creation rejected by Judge0. Status={StatusCode} Body={Body}, URL={Url}",
                    (int)response.StatusCode, body, $"{httpClient.BaseAddress}submissions");
                throw new InvalidOperationException(
                    $"Judge0 rejected the submission (HTTP {(int)response.StatusCode}).");
            }

            var created = await response.Content.ReadFromJsonAsync<Judge0CreatedResponse>(cancellationToken)
                ?? throw new InvalidOperationException("Judge0 returned an empty submission response.");

            logger.LogInformation("✅ [JUDGE0-CLIENT] Submission accepted by Judge0: Token={Token}", created.Token);
            return created.Token;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "🔴 [JUDGE0-CLIENT] HTTP request to Judge0 FAILED! URL={Url}, Message={Message}, InnerException={InnerException}, StatusCode={StatusCode}", 
                $"{httpClient.BaseAddress}submissions", ex.Message, ex.InnerException?.Message ?? "None", ex.StatusCode);
            throw;
        }
    }

    private async Task<Judge0SubmissionResult> PollUntilCompleteAsync(
        string token, CancellationToken cancellationToken)
    {
        logger.LogInformation("🔄 [JUDGE0-CLIENT] Starting to poll for submission: Token={Token}, MaxAttempts={MaxAttempts}, IntervalMs={IntervalMs}", 
            token, _options.MaxPollAttempts, _options.PollIntervalMs);
        
        for (var attempt = 1; attempt <= _options.MaxPollAttempts; attempt++)
        {
            var result = await FetchSubmissionAsync(token, cancellationToken);

            if (Judge0Status.IsTerminal(result.StatusId))
            {
                logger.LogInformation("✅ [JUDGE0-CLIENT] Submission reached terminal status: Token={Token}, Attempt={Attempt}, StatusId={StatusId}, Status={Status}", 
                    token, attempt, result.StatusId, result.StatusDescription);
                return result;
            }

            logger.LogDebug(
                "⏳ [JUDGE0-CLIENT] Submission still processing: Token={Token}, Status={Status}, Attempt={Attempt}/{Max}",
                token, result.StatusDescription, attempt, _options.MaxPollAttempts);

            await Task.Delay(_options.PollIntervalMs, cancellationToken);
        }

        logger.LogWarning(
            "⚠️  [JUDGE0-CLIENT] Submission polling TIMED OUT! Token={Token}, MaxAttempts={MaxAttempts}. Returning PollTimedOut result.",
            token, _options.MaxPollAttempts);

        return new Judge0SubmissionResult
        {
            StatusId = Judge0Status.Processing,
            StatusDescription = "Polling budget exhausted",
            PollTimedOut = true,
            Token = token
        };
    }

    private async Task<Judge0SubmissionResult> FetchSubmissionAsync(
        string token, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"submissions/{token}?base64_encoded=false&fields=stdout,stderr,compile_output,message,status,time,memory";
            
            using var response = await httpClient.GetAsync(url, cancellationToken);

            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadFromJsonAsync<Judge0StatusResponse>(cancellationToken)
                ?? throw new InvalidOperationException("Judge0 returned an empty status response.");

            logger.LogDebug("📥 [JUDGE0-CLIENT] Fetched submission status: Token={Token}, StatusId={StatusId}, Status={Status}", 
                token, body.Status?.Id ?? 0, body.Status?.Description ?? "Unknown");

            return new Judge0SubmissionResult
            {
                StatusId = body.Status?.Id ?? 0,
                StatusDescription = body.Status?.Description ?? "Unknown",
                Stdout = body.Stdout,
                Stderr = body.Stderr,
                CompileOutput = body.CompileOutput,
                Message = body.Message,
                TimeSeconds = body.Time,
                MemoryKb = body.Memory,
                Token = token
            };
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "🔴 [JUDGE0-CLIENT] Failed to fetch submission status! Token={Token}, URL={Url}, Message={Message}, InnerException={InnerException}", 
                token, $"{httpClient.BaseAddress}submissions/{token}", ex.Message, ex.InnerException?.Message ?? "None");
            throw;
        }
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
