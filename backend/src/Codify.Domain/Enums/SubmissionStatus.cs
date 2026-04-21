namespace Codify.Domain.Enums;

public enum SubmissionStatus
{
    Pending,
    Running,
    Accepted,
    WrongAnswer,
    RuntimeError,
    TimeLimitExceeded,
    CompileError
}
