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

    public async Task<IReadOnlyList<Judge0SubmissionResult>> ExecuteBatchAsync(
        IEnumerable<Judge0SubmissionRequest> requests,
        CancellationToken cancellationToken = default)
    {
        var requestList = requests.ToList();
        logger.LogInformation("🎯 [JUDGE0-CLIENT-BATCH] Starting batch execution: Count={Count}, BaseAddress={BaseAddress}", 
            requestList.Count, httpClient.BaseAddress);
        
        if (requestList.Count == 0)
        {
            logger.LogWarning("⚠️  [JUDGE0-CLIENT-BATCH] Empty batch request, returning empty results");
            return Array.Empty<Judge0SubmissionResult>();
        }

        try
        {
            var tokens = await CreateBatchSubmissionsAsync(requestList, cancellationToken);
            logger.LogInformation("✅ [JUDGE0-CLIENT-BATCH] Batch submissions created: Tokens={Tokens}", string.Join(", ", tokens));
            
            var results = await PollBatchUntilCompleteAsync(tokens, cancellationToken);
            logger.LogInformation("✅ [JUDGE0-CLIENT-BATCH] Batch execution completed: TotalCount={Total}, Succeeded={Succeeded}, Failed={Failed}", 
                results.Count, results.Count(r => !r.PollTimedOut), results.Count(r => r.PollTimedOut));
            
            return results;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "🔴 [JUDGE0-CLIENT-BATCH] Batch execution FAILED! ErrorType={ErrorType}, Message={Message}", 
                ex.GetType().Name, ex.Message);
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
        logger.LogInformation("🔄 [JUDGE0-CLIENT] Starting to poll for submission: Token={Token}, MaxAttempts={MaxAttempts}, InitialIntervalMs={IntervalMs}", 
            token, _options.MaxPollAttempts, _options.PollIntervalMs);
        
        // Adaptive polling: start fast, then back off
        var intervalMs = Math.Max(150, _options.PollIntervalMs / 4); // Start at 25% of configured interval (min 150ms)
        
        for (var attempt = 1; attempt <= _options.MaxPollAttempts; attempt++)
        {
            var result = await FetchSubmissionAsync(token, cancellationToken);

            if (Judge0Status.IsTerminal(result.StatusId))
            {
                logger.LogInformation("✅ [JUDGE0-CLIENT] Submission reached terminal status: Token={Token}, Attempt={Attempt}, StatusId={StatusId}, Status={Status}, TotalTimeMs={TotalTime}", 
                    token, attempt, result.StatusId, result.StatusDescription, attempt * intervalMs);
                return result;
            }

            logger.LogDebug(
                "⏳ [JUDGE0-CLIENT] Submission still processing: Token={Token}, Status={Status}, Attempt={Attempt}/{Max}, NextPollIn={NextPoll}ms",
                token, result.StatusDescription, attempt, _options.MaxPollAttempts, intervalMs);

            await Task.Delay(intervalMs, cancellationToken);
            
            // Exponential backoff: double the interval each time, cap at configured max
            intervalMs = Math.Min(intervalMs * 2, _options.PollIntervalMs);
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

    private async Task<IReadOnlyList<string>> CreateBatchSubmissionsAsync(
        List<Judge0SubmissionRequest> requests, CancellationToken cancellationToken)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine($"[BATCH-CREATE] Starting batch submission creation");
        Console.WriteLine($"[BATCH-CREATE] Request count: {requests.Count}");
        Console.WriteLine($"[BATCH-CREATE] Judge0 URL: {httpClient.BaseAddress}submissions/batch?base64_encoded=false");
        
        var payloads = requests.Select((r, index) => {
            Console.WriteLine($"[BATCH-CREATE] Request {index + 1}:");
            Console.WriteLine($"  - LanguageId: {r.LanguageId}");
            Console.WriteLine($"  - CpuTimeLimit: {r.CpuTimeLimitSeconds}s");
            Console.WriteLine($"  - MemoryLimit: {r.MemoryLimitKb}KB");
            Console.WriteLine($"  - Stdin length: {r.Stdin?.Length ?? 0}");
            Console.WriteLine($"  - Stdin content: [{r.Stdin?.Replace("\n", "\\n")}]");
            Console.WriteLine($"  - SourceCode length: {r.SourceCode.Length}");
            Console.WriteLine($"  - SourceCode preview (first 200 chars): {(r.SourceCode.Length > 200 ? r.SourceCode.Substring(0, 200) : r.SourceCode)}");
            return new Judge0CreatePayload(
                r.SourceCode,
                r.LanguageId,
                r.Stdin,
                r.CpuTimeLimitSeconds,
                r.MemoryLimitKb);
        }).ToList();

        try
        {
            // Judge0 batch API expects submissions wrapped in object with "submissions" key
            var batchPayload = new { submissions = payloads };
            
            // Log full JSON payload
            var jsonPayload = System.Text.Json.JsonSerializer.Serialize(batchPayload);
            Console.WriteLine($"[BATCH-CREATE] FULL JSON PAYLOAD:");
            Console.WriteLine(jsonPayload);
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            
            using var response = await httpClient.PostAsJsonAsync(
                "submissions/batch?base64_encoded=false", batchPayload, cancellationToken);

            Console.WriteLine($"[BATCH-CREATE] Response status: {response.StatusCode}");
            
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                Console.WriteLine($"[BATCH-CREATE] ERROR! Judge0 rejected batch:");
                Console.WriteLine($"  Status: {(int)response.StatusCode}");
                Console.WriteLine($"  Body: {body}");
                throw new InvalidOperationException(
                    $"Judge0 rejected the batch submission (HTTP {(int)response.StatusCode}): {body}");
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"[BATCH-CREATE] SUCCESS! Judge0 response body:");
            Console.WriteLine(responseBody);
            
            var batchResponse = System.Text.Json.JsonSerializer.Deserialize<List<Judge0CreatedResponse>>(responseBody)
                ?? throw new InvalidOperationException("Judge0 returned an empty batch response.");

            var tokens = batchResponse.Select(r => r.Token).ToList();
            Console.WriteLine($"[BATCH-CREATE] Received {tokens.Count} tokens:");
            foreach (var token in tokens)
            {
                Console.WriteLine($"  - {token}");
            }
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            return tokens;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "🔴 [JUDGE0-CLIENT-BATCH] HTTP request to Judge0 FAILED! Message={Message}", ex.Message);
            throw;
        }
    }

    private async Task<IReadOnlyList<Judge0SubmissionResult>> PollBatchUntilCompleteAsync(
        IReadOnlyList<string> tokens, CancellationToken cancellationToken)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine($"[BATCH-POLL] Starting batch polling");
        Console.WriteLine($"[BATCH-POLL] Token count: {tokens.Count}");
        Console.WriteLine($"[BATCH-POLL] Max attempts: {_options.MaxPollAttempts}");
        
        var results = new Dictionary<string, Judge0SubmissionResult>();
        var pendingTokens = new HashSet<string>(tokens);
        
        // Adaptive polling for batch
        var intervalMs = Math.Max(150, _options.PollIntervalMs / 4);
        
        for (var attempt = 1; attempt <= _options.MaxPollAttempts && pendingTokens.Count > 0; attempt++)
        {
            Console.WriteLine($"\n[BATCH-POLL] ────── Attempt {attempt}/{_options.MaxPollAttempts} ──────");
            Console.WriteLine($"[BATCH-POLL] Pending tokens: {pendingTokens.Count}");
            Console.WriteLine($"[BATCH-POLL] Completed: {results.Count}");
            
            // Fetch all pending tokens in one request
            var tokenQuery = string.Join(",", pendingTokens);
            var url = $"submissions/batch?tokens={tokenQuery}&base64_encoded=false&fields=token,stdout,stderr,compile_output,message,status,time,memory";
            
            try
            {
                Console.WriteLine($"[BATCH-POLL] Polling URL: {httpClient.BaseAddress}{url}");
                
                using var response = await httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
                Console.WriteLine($"[BATCH-POLL] Raw JSON response:");
                Console.WriteLine(rawJson);

                // Deserialize from the string we just read
                var submissions = System.Text.Json.JsonSerializer.Deserialize<Judge0BatchStatusResponse>(rawJson)
                    ?? throw new InvalidOperationException("Judge0 returned an empty batch status response.");

                Console.WriteLine($"[BATCH-POLL] Parsed {submissions.Submissions?.Length ?? 0} submissions from response");

                foreach (var sub in submissions.Submissions ?? Array.Empty<Judge0StatusResponse>())
                {
                    if (sub.Token == null) continue;
                    
                    Console.WriteLine($"\n[BATCH-POLL] Token: {sub.Token}");
                    Console.WriteLine($"  StatusId: {sub.Status?.Id ?? 0}");
                    Console.WriteLine($"  Status: {sub.Status?.Description ?? "Unknown"}");
                    Console.WriteLine($"  Time: {sub.Time ?? "(null)"}");
                    Console.WriteLine($"  Memory: {sub.Memory?.ToString() ?? "(null)"}KB");
                    Console.WriteLine($"  Stdout length: {sub.Stdout?.Length ?? 0}");
                    Console.WriteLine($"  Stdout content: [{sub.Stdout}]");
                    Console.WriteLine($"  Stderr length: {sub.Stderr?.Length ?? 0}");
                    Console.WriteLine($"  Stderr content: [{sub.Stderr}]");
                    
                    var result = new Judge0SubmissionResult
                    {
                        StatusId = sub.Status?.Id ?? 0,
                        StatusDescription = sub.Status?.Description ?? "Unknown",
                        Stdout = sub.Stdout,
                        Stderr = sub.Stderr,
                        CompileOutput = sub.CompileOutput,
                        Message = sub.Message,
                        TimeSeconds = sub.Time,
                        MemoryKb = sub.Memory,
                        Token = sub.Token
                    };

                    Console.WriteLine($"  IsTerminal: {Judge0Status.IsTerminal(result.StatusId)}");
                    
                    if (Judge0Status.IsTerminal(result.StatusId))
                    {
                        results[sub.Token] = result;
                        pendingTokens.Remove(sub.Token);
                        Console.WriteLine($"  ✅ COMPLETED! Pending count now: {pendingTokens.Count}");
                    }
                    else
                    {
                        Console.WriteLine($"  ⏳ Still processing...");
                    }
                }

                if (pendingTokens.Count == 0)
                {
                    Console.WriteLine($"\n[BATCH-POLL] ✅ ALL SUBMISSIONS COMPLETED at attempt {attempt}");
                    break;
                }

                Console.WriteLine($"\n[BATCH-POLL] ⏳ Waiting {intervalMs}ms before next poll...");

                await Task.Delay(intervalMs, cancellationToken);
                intervalMs = Math.Min(intervalMs * 2, _options.PollIntervalMs);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"\n[BATCH-POLL] ❌ HTTP ERROR on attempt {attempt}: {ex.Message}");
                
                if (attempt >= _options.MaxPollAttempts)
                    throw;
                    
                await Task.Delay(intervalMs, cancellationToken);
                intervalMs = Math.Min(intervalMs * 2, _options.PollIntervalMs);
            }
        }

        // Handle any remaining pending tokens as timed out
        if (pendingTokens.Count > 0)
        {
            Console.WriteLine($"\n[BATCH-POLL] ⚠️  POLLING TIMED OUT for {pendingTokens.Count} submissions:");
            foreach (var token in pendingTokens)
            {
                Console.WriteLine($"  - {token}");
                results[token] = new Judge0SubmissionResult
                {
                    StatusId = Judge0Status.Processing,
                    StatusDescription = "Polling budget exhausted",
                    PollTimedOut = true,
                    Token = token
                };
            }
        }

        // Return results in the same order as input tokens
        Console.WriteLine($"\n[BATCH-POLL] Returning {results.Count} results in original order");
        Console.WriteLine("═══════════════════════════════════════════════════════════════\n");
        return tokens.Select(t => results[t]).ToList();
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

    private record Judge0BatchPayload(
        [property: JsonPropertyName("submissions")] List<Judge0CreatePayload> Submissions);

    private record Judge0CreatedResponse(
        [property: JsonPropertyName("token")] string Token);

    private record Judge0StatusResponse(
        [property: JsonPropertyName("token")] string? Token,
        [property: JsonPropertyName("stdout")] string? Stdout,
        [property: JsonPropertyName("stderr")] string? Stderr,
        [property: JsonPropertyName("compile_output")] string? CompileOutput,
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("time")] string? Time,
        [property: JsonPropertyName("memory")] int? Memory,
        [property: JsonPropertyName("status")] Judge0StatusPayload? Status);

    private record Judge0BatchStatusResponse(
        [property: JsonPropertyName("submissions")] Judge0StatusResponse[]? Submissions);

    private record Judge0StatusPayload(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("description")] string Description);
}
