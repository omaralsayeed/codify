# Codify.Application

> **Use cases and business logic.** Orchestrates domain entities and infrastructure.

## What Goes Here

- `Services/` — One service per module (AuthService, ProblemService, SubmissionService, etc.)
- `Interfaces/` — IAuthService, IProblemService, ISubmissionService, IExecutionService, agent interfaces
- `DTOs/` — Request and Response objects for each use case
- `Agents/` — ITutorAgent, ICodeCheckerAgent, IAnalyticsAgent

## Rules

- Services call repositories and agents, never EF Core directly
- Services return DTOs, not domain entities
- All methods are async
- Services handle the "what happens when" logic — not the controllers

## Service Interface Template

```csharp
// Interfaces/ISubmissionService.cs
public interface ISubmissionService
{
    Task<SubmissionResponse> SubmitAsync(CreateSubmissionRequest request, Guid userId);
    Task<SubmissionResponse> GetByIdAsync(Guid submissionId, Guid requestingUserId);
    Task<IEnumerable<SubmissionSummaryResponse>> GetByProblemAsync(Guid problemId, Guid userId);
}
```

## Agent Interface Template

```csharp
// Agents/ITutorAgent.cs
public interface ITutorAgent
{
    Task<HintResponse> GenerateHintAsync(HintRequest request);
}
```

## DTO Naming

```
Create<Entity>Request   — incoming body for POST
Update<Entity>Request   — incoming body for PUT
<Entity>Response        — outgoing response shape
<Entity>SummaryResponse — lighter version for list views
```

## See Also

Full agent I/O contracts: [AGENTS.md](../../../../docs/agents/AGENTS.md)
