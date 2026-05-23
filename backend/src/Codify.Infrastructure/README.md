# Codify.Infrastructure

> **External concerns.** Database, LLM clients, vector DB, Docker client.

## What Goes Here

- `Persistence/` — EF Core DbContext, entity configurations (Fluent API), migrations
- `Repositories/` — Implements repository interfaces from Application layer
- `AI/` — LLM client, Chroma client, RAG pipeline, prompt loading
- `AI/Prompts/` — Prompt template `.txt` files (not hardcoded in code)
- `Docker/` — Docker API client for execution engine

## Rules

- Implement interfaces defined in `Codify.Application`
- EF Core Fluent API configurations live in `Persistence/Configurations/`
- Prompt templates are `.txt` files loaded at runtime — never string literals in code
- Never log full code submissions to external services

## Repository Template

```csharp
// Repositories/SubmissionRepository.cs
public class SubmissionRepository : ISubmissionRepository
{
    private readonly CodifyDbContext _db;

    public SubmissionRepository(CodifyDbContext db) => _db = db;

    public async Task<Submission?> GetByIdAsync(Guid id)
        => await _db.Submissions.FindAsync(id);

    public async Task AddAsync(Submission submission)
    {
        await _db.Submissions.AddAsync(submission);
        await _db.SaveChangesAsync();
    }
}
```

## Prompt File Convention

```
AI/Prompts/
├── tutor-agent-system.txt
├── code-checker-agent-system.txt
└── analytics-agent-system.txt
```

Load with:
```csharp
var prompt = await File.ReadAllTextAsync("AI/Prompts/tutor-agent-system.txt");
```

## See Also

- [AGENTS.md](../../../../docs/agents/AGENTS.md) for agent I/O and prompt templates
- [ENV_SETUP.md](../../../../ENV_SETUP.md) for LLM and DB configuration
