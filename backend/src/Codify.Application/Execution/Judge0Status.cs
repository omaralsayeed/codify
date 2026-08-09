namespace Codify.Application.Execution;

/// <summary>
/// Judge0's status id reference (https://ce.judge0.com/#statuses-and-languages-status-get).
/// Ids 7–12 are all runtime-error subtypes (SIGSEGV, SIGXFSZ, SIGFPE, SIGABRT, NZEC, Other).
/// </summary>
public static class Judge0Status
{
    public const int InQueue = 1;
    public const int Processing = 2;
    public const int Accepted = 3;
    public const int WrongAnswer = 4;
    public const int TimeLimitExceeded = 5;
    public const int CompilationError = 6;
    public const int RuntimeErrorRangeStart = 7;
    public const int RuntimeErrorRangeEnd = 12;
    public const int InternalError = 13;
    public const int ExecFormatError = 14;

    public static bool IsQueuedOrProcessing(int statusId) =>
        statusId is InQueue or Processing;

    public static bool IsTerminal(int statusId) => !IsQueuedOrProcessing(statusId);

    public static bool IsRuntimeError(int statusId) =>
        statusId is >= RuntimeErrorRangeStart and <= RuntimeErrorRangeEnd;
}
