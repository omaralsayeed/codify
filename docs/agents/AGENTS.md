# Codify â€” AI Agent Design & Contracts

This document reflects the AI implementation that is actually wired into the backend.
There are three AI agents. Each has a clearly-scoped responsibility, a defined
input/output contract, and uses the shared LLM + Chroma Cloud RAG foundation.

| Agent | Type | Trigger | RAG |
|-------|------|---------|-----|
| **Tutor Agent** | Agentic (LLM tool calling) | `POST /api/hints` | Yes â€” `search_knowledge_base` |
| **Code Analysis Agent** | Static workflow (fixed pipeline) | Fired after submission evaluation | No (deterministic heuristics) |
| **Tagging Agent** | Static workflow (fixed pipeline) | Fired on progress + auto-scan / endpoint | Yes â€” concept grounding |

## Design Principles

1. **Agentic where it matters.** Only the Tutor Agent uses open-ended LLM tool calling;
   the model decides which tools to call. The Code Analysis and Tagging agents are
   deterministic, static workflows (a single LLM call inside a fixed pipeline) because
   their steps are known in advance.
2. **Structured output only.** Every agent returns JSON that the backend validates.
   Malformed or missing output falls back to a safe default â€” never a crash.
3. **Grounded, no hallucination.** Tools and retrieved Chroma context ground the model
   in real data. Prompts instruct the model not to invent unavailable information.
4. **Deterministic-first.** Measurable signals (submission history, code heuristics,
   per-tag success rates) are computed in C#; the LLM reasons over them.
5. **Graceful degradation.** Any LLM/Chroma failure returns empty context or a fallback
   result so the agents never break the request or evaluation pipeline.
6. **No secrets in code.** All keys/endpoints come from configuration / environment.

## Shared Foundation

### LLM tool calling
`ILLMClient.CompleteAsync` (single round-trip) and `ILLMClient.CompleteWithToolsAsync`
(one step of the tool loop) are implemented by `OpenAiChatClient` using the OpenAI SDK.
Shared message/tool types live in `Codify.Application/Interfaces/LlmContracts.cs`.

### Chroma Cloud RAG
Retrieval-only (documents are pre-populated in Chroma Cloud; nothing is ingested from
local files at runtime).

```
query -> IEmbeddingService (text-embedding-3-small)
      -> IVectorStore / ChromaCloudVectorStore (v2 REST, Bearer auth, tenant+database path)
      -> IKnowledgeBaseSearchService -> agent context
```

- `ChromaCloudOptions` holds Endpoint / ApiKey / Tenant / Database / CollectionName /
  SimilarityThreshold, bound from the `ChromaCloud` config section.
- `ChromaCloudVectorStore` resolves the collection id (get-or-create), builds metadata
  `where` filters, parses batch query responses, and returns an empty list on any failure.
- Expected document metadata: `source` (`"concept"`) and `concept_tag`.

## Tutor Agent (agentic)

Progressive hints without revealing the solution. The model decides which tools to call:

| Tool | Purpose |
|------|---------|
| `get_attempt_history` | Real attempt count / statuses / prior hint levels. |
| `search_knowledge_base` | RAG grounding from Chroma when a concept is missing. |
| `check_partial_code` | Lightweight static observations on the student's code. |
| `get_previous_hints` | Avoid repeating guidance. |

Loop capped at 5 iterations; hint level clamped to 1..3; `tools_used` +
`reasoning_summary` persisted to `HintLog` (`ToolsUsedJson`, `ReasoningSummary`).

## Code Analysis Agent (static workflow)

Fired after a submission is evaluated (via `ICodeCheckerAgent`, wired into
`JudgeEvaluationService`). Fixed pipeline:

```
code -> CodeAnalysisHeuristics (deterministic signals)
     -> single LLM call (code-analysis-agent-system.txt)
     -> feedback items + AI-generated verdict
     -> FeedbackRecord rows
```

Also detects likely **AI-generated code** (confidence threshold 0.6) and emits a
`FeedbackType.AiGenerated` record when confident.

## Tagging Agent (static workflow)

Classifies a problem's concept tags, grounded in RAG. Fixed pipeline:

```
problem statement -> IKnowledgeBaseSearchService (concept grounding)
                  -> single LLM call (tagging-agent-system.txt)
                  -> validate tags against the allowed ConceptTag list
                  -> apply ProblemTags
```

Fired two ways:
- **On progress:** `JudgeEvaluationService` calls `ITaggingService.UpdateUserTagsOnProgressAsync`
  to refresh a student's weak/strong topic profile (reuses `IPerformanceService`).
- **Tagging untagged problems:** `POST /api/ai/tagging/{problemId}` (one problem),
  `POST /api/ai/tagging/scan` (all), plus an automatic startup scan
  (`Tagging:AutoTagUntaggedOnStartup`, only when an OpenAI key is configured).

## Deterministic Analytics (not an agent)

The student/instructor dashboards (`AnalyticsService` / `AnalyticsController`) compute
statistics deterministically and are **not** AI agents. They are left unchanged.

