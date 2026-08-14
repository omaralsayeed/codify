# Tagging Agent

**Type:** Static workflow (fixed pipeline, single LLM call). Not agentic.
**Triggers:** fired on student progress; tags untagged problems via endpoint or auto-scan.
**Contracts:** `ITaggingAgent.ClassifyProblemTagsAsync(...)` + `ITaggingService`.

## Purpose
1. Classify a problem's concept tags, grounded in RAG concept context.
2. Refresh a student's weak/strong concept-tag profile whenever they make progress.
3. Automatically tag all currently-untagged problems (scan).

## Flow (fixed pipeline)

```
TaggingService.TagProblemAsync(problemId)
  -> load problem; if already tagged, return unchanged
  -> load all ConceptTags (allowed list)
  -> ITaggingAgent.ClassifyProblemTagsAsync
       1. IKnowledgeBaseSearchService: RAG concept grounding (Chroma)
       2. render tagging-agent-system.txt (available tags + context + problem)
       3. single ILLMClient.CompleteAsync call
       4. validate assigned tags against the allowed list
  -> apply ProblemTags + SaveChanges
```

## Triggers
| Trigger | Mechanism |
|---------|-----------|
| On student progress | `JudgeEvaluationService` -> `ITaggingService.UpdateUserTagsOnProgressAsync` (reuses `IPerformanceService.UpdateAfterSubmissionAsync`). |
| Tag one problem | `POST /api/ai/tagging/{problemId}` (Instructor, rate-limited `ai-tagging`). |
| Tag all untagged | `POST /api/ai/tagging/scan` (Instructor) **and** an automatic startup scan. |

The startup auto-scan (`Tagging:AutoTagUntaggedOnStartup`) runs fire-and-forget after
seeding, but only when an OpenAI key is configured; it never blocks startup and every
per-problem failure is isolated.

## Output contract
```json
{
  "assignedTags": ["Dynamic Programming", "Graphs"],
  "confidence": 0.8,
  "reasoning": "..."
}
```
Only tags present in the allowed ConceptTag list are applied (validated server-side).

## Guard rails
1. Tags are validated against the allowed list; invented tags are dropped.
2. RAG failure -> classification proceeds without context.
3. LLM failure / invalid JSON -> empty classification (problem left untagged).
4. A single problem failing never aborts the whole scan.

## Key files
- `Codify.Application/Services/TaggingService.cs` — orchestration + scan + progress hook.
- `Codify.Infrastructure/AI/TaggingAgentService.cs` — RAG + single LLM classification.
- `Codify.Application/Agents/ITaggingAgent.cs`, `Codify.Application/Interfaces/ITaggingService.cs`.
- Prompt: `Codify.Infrastructure/AI/Prompts/tagging-agent-system.txt`.
- Endpoints: `Codify.API/Controllers/AiController.cs`.
