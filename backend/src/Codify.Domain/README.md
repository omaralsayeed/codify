# Codify.Domain

> **Core entities and rules.** No dependencies on EF Core, HTTP, or any external library.

## What Goes Here

- `Entities/` — User, Problem, Submission, HintLog, PerformanceProfile, etc.
- `Enums/` — UserRole, Difficulty, SubmissionStatus, FeedbackType, etc.
- `Exceptions/` — NotFoundException, ForbiddenException, ValidationException

## Rules

- No EF Core attributes in entities (use Fluent API in Infrastructure instead)
- No HTTP or serialization concerns
- Pure C# only — this layer has zero external dependencies

## Entity Example

```csharp
// Entities/Submission.cs
public class Submission
{
    public Guid Id { get; private set; }
    public Guid ProblemId { get; private set; }
    public Guid UserId { get; private set; }
    public string Code { get; private set; }
    public SubmissionLanguage Language { get; private set; }
    public SubmissionStatus Status { get; private set; }
    public DateTime SubmittedAt { get; private set; }

    // Domain rule: status transitions
    public void MarkAsRunning() => Status = SubmissionStatus.Running;
    public void MarkAsAccepted() => Status = SubmissionStatus.Accepted;
    public void MarkAsFailed(SubmissionStatus reason) => Status = reason;
}
```

## See Also

Full entity definitions: [DATA_MODEL.md](../../../../docs/database/DATA_MODEL.md)
