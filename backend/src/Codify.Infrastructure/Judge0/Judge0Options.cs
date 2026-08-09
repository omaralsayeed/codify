namespace Codify.Infrastructure.Judge0;

public class Judge0Options
{
    public const string SectionName = "Judge0";

    /// <summary>
    /// Base URL of the Judge0 instance, e.g. "http://localhost:2358" for a self-hosted
    /// docker-compose instance, or "https://judge0-ce.p.rapidapi.com" for RapidAPI.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:2358";

    /// <summary>RapidAPI key. Leave empty for a self-hosted Judge0 instance with no auth.</summary>
    public string? ApiKey { get; set; }

    /// <summary>RapidAPI host header (e.g. "judge0-ce.p.rapidapi.com"). Leave empty for self-hosted.</summary>
    public string? ApiHost { get; set; }

    /// <summary>Delay between polling attempts while a submission is still queued/processing.</summary>
    public int PollIntervalMs { get; set; } = 1000;

    /// <summary>Maximum number of polling attempts before giving up on a submission.</summary>
    public int MaxPollAttempts { get; set; } = 15;
}
