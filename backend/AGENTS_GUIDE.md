# Codify AI Agents — Full Reference

This document covers every AI agent in the Codify platform: what it does, how it works, which files implement it, how it's wired into the API, and where its configuration lives. There is no Python anywhere — every agent runs inside the .NET backend and talks to OpenAI directly over HTTP.

---

## Quick Map

| Agent | Type | Trigger | Model |
|---|---|---|---|
| Tutor Agent | Agentic (tool-calling loop) | Student requests a hint | gpt-4o-mini → gpt-4o (escalation) |
| Code Analysis Agent | Static workflow | After every submission evaluation | gpt-4o-mini |
| Tagging Agent | Static workflow | Manual (instructor) + every submission + user progress | gpt-4o-mini |

---

## Shared Infrastructure

All three agents share the same LLM client, prompt loader, and DI wiring.

### LLM Client

| Item | Path |
|---|---|
| Interface | `src/Codify.Application/Interfaces/ILLMClient.cs` |
| Implementation | `src/Codify.Infrastructure/AI/OpenAiChatClient.cs` |
| Config model | `src/Codify.Infrastructure/AI/OpenAiOptions.cs` |

`OpenAiChatClient` wraps the official `OpenAI` .NET SDK. It caches one `ChatClient` instance per model name (thread-safe), handles both simple `CompleteAsync` calls (one system + one user message) and the full tool-calling loop via `CompleteWithToolsAsync`. It also supports a custom `BaseUrl` so calls can be routed through a proxy (currently pointed at the ITI gateway: `http://apiaccess.iti.net.eg/api/v1/student/chat`).

The interface exposes three methods:
- `CompleteAsync` — plain completion, used by Code Analysis Agent and Tagging Agent
- `CompleteWithToolsAsync` — tool-calling loop, used by Tutor Agent
- `CompleteWithToolsAsync(modelOverride)` — same but lets the caller swap models mid-conversation (used for escalation)

### Message / Tool contracts

| Item | Path |
|---|---|
| `LlmMessage`, `LlmToolCall`, `LlmToolDefinition`, `LlmResponse` | `src/Codify.Application/Interfaces/LlmContracts.cs` |

These are the DTOs that travel between the agent services and the LLM client. `LlmMessage` can carry tool-call requests (when `Role == "assistant"`) or tool results (when `Role == "tool"`).

### Prompt templates

All system prompts live as plain `.txt` files loaded at runtime via `IPromptLoader`:

| Template | Path |
|---|---|
| Tutor Agent system prompt | `src/Codify.Infrastructure/AI/Prompts/tutor-agent-system.txt` |
| Code Analysis Agent system prompt | `src/Codify.Infrastructure/AI/Prompts/code-analysis-agent-system.txt` |
| Tagging Agent system prompt | `src/Codify.Infrastructure/AI/Prompts/tagging-agent-system.txt` |

Templates use `{{placeholder}}` syntax. `PromptTemplate.Render()` does a simple string substitution before the prompt is sent.

### Dependency Injection

Everything is registered in `src/Codify.Infrastructure/DependencyInjection.cs`:

```
ILLMClient          → OpenAiChatClient          (singleton, per-model client caching)
IPromptLoader       → PromptLoader              (singleton)
IEmbeddingService   → OpenAiEmbeddingService    (scoped, uses text-embedding-3-small)
IVectorStore        → ChromaCloudVectorStore    (scoped)
IKnowledgeBaseSearchService  → KnowledgeBaseSearchService   (scoped)
IKnowledgeBaseIngestionService → KnowledgeBaseIngestionService (scoped)

ITutorAgentTools    → TutorAgentTools           (scoped)
ITutorAgent         → TutorAgentService         (scoped)

ICodeCheckerAgent   → CodeAnalysisAgentService  (scoped)

ITaggingAgent       → TaggingAgentService       (scoped)
ITaggingService     → TaggingService            (scoped)
```

### Configuration (`src/Codify.API/appsettings.json`)

```json
"OpenAI": {
  "ApiKey": "...",
  "Model": "gpt-4o-mini",
  "EscalationModel": "gpt-4o",
  "EscalationAttemptThreshold": 3,
  "EscalationHintLevelThreshold": 2,
  "EmbeddingModel": "text-embedding-3-small",
  "BaseUrl": "http://apiaccess.iti.net.eg/api/v1/student/chat"
},
"ChromaCloud": {
  "Endpoint": "https://api.trychroma.com",
  "ApiKey": "...",
  "Tenant": "...",
  "Database": "codify",
  "CollectionName": "codify-knowledge-base",
  "SimilarityThreshold": 0.25,
  "TimeoutSeconds": 20
}
```

The `BaseUrl` field routes all OpenAI calls through the ITI student gateway instead of the official `api.openai.com` endpoint. If `BaseUrl` is blank, the standard OpenAI URL is used automatically.

### RAG / Vector DB (Chroma Cloud)

The Tagging Agent and Tutor Agent both search a vector knowledge base hosted on Chroma Cloud. The knowledge base holds pre-embedded chunks of DSA concept explanations keyed by concept tag (e.g., "Dynamic Programming", "Hash Map"). Embeddings are generated with OpenAI's `text-embedding-3-small`. Similarity threshold is `0.25` — chunks below that score are filtered out.

---

## Agent 1 — Tutor Agent

### What it does

Generates personalized, Socratic hints for students who are stuck on a problem. It never gives the full solution — it asks guiding questions and escalates specificity gradually across up to 3 hint levels. The agent is *agentic*: it decides for itself which tools to call (if any) before writing the hint, based on what context it actually needs.

### Architecture — agentic tool-calling loop

```
Student requests hint
    ↓
AiHintService builds TutorAgentInput
    ↓
TutorAgentService sends [system prompt + user message + tool definitions] to LLM
    ↓
Model responds with tool_calls (or final text)
    ↓  (if tool_calls)
TutorAgentTools executes the requested tools
Results appended as "tool" messages
    ↓
Resend full conversation to model
    ↓  (repeat up to 5 iterations)
Model returns final JSON hint
    ↓
AiHintService persists the hint log and returns HintResponse to the client
```

The loop is capped at **5 iterations** to control cost and latency.

### Files

| Role | Path |
|---|---|
| Interface | `src/Codify.Application/Agents/ITutorAgent.cs` |
| Input DTO | `src/Codify.Application/Agents/TutorAgentInput.cs` |
| Implementation | `src/Codify.Infrastructure/AI/TutorAgentService.cs` |
| System prompt | `src/Codify.Infrastructure/AI/Prompts/tutor-agent-system.txt` |
| Tool interface | `src/Codify.Application/Agents/ITutorAgentTools.cs` |
| Tool implementation | `src/Codify.Infrastructure/AI/TutorAgentTools.cs` |
| Tool schemas & dispatcher | `src/Codify.Infrastructure/AI/TutorAgentToolSchemas.cs` |
| Orchestrator service | `src/Codify.Application/Services/AiHintService.cs` |
| Service interface | `src/Codify.Application/Interfaces/IAiHintService.cs` |
| Request / Response DTOs | `src/Codify.Application/DTOs/AI/HintRequest.cs` `src/Codify.Application/DTOs/AI/HintResponse.cs` |
| API controller | `src/Codify.API/Controllers/HintsController.cs` |

### Tools the model can call

| Tool name | What it returns |
|---|---|
| `get_attempt_history` | Submission count, past statuses, previous hint levels, timestamps — tells the model how stuck the student really is |
| `search_knowledge_base` | Top-K chunks from Chroma Cloud for a natural language query — grounds conceptual hints in real content |
| `check_partial_code` | Lightweight static observations on the student's code (loop present? recursion? base case? syntax issues?) |
| `get_previous_hints` | Exact text of all prior hints for this student on this problem — prevents repetition |

The model calls zero, one, or multiple tools in any order. The tool schemas (JSON Schema format, OpenAI function-calling) are defined in `TutorAgentToolSchemas.cs` alongside the dispatcher that routes a model-requested tool call to the right `ITutorAgentTools` method.

### Model escalation

By default the agent uses `gpt-4o-mini` (fast, cheap). If `AttemptCount >= 3` AND `HintLevel >= 2`, it escalates to `gpt-4o` for better multi-step reasoning. Both thresholds are configurable in `appsettings.json`.

### API surface

```
POST /api/hints          — request a hint (Student role, rate-limited: 10/hour)
GET  /api/hints/history  — get hint history for a problem (Student role, no rate limit)
```

Route aliases: `/api/ai/hints` also resolves to the same controller.

### Response shape

`HintResponse` includes:
- `hintText` — the hint itself
- `hintLevel` — the agent's own 1–3 assessment
- `followUpQuestion` — a guiding question to keep the student thinking
- `hasMoreHints` — whether levels remain
- `toolsUsed` — names of every tool the agent called (agentic evidence)
- `reasoningSummary` — internal note for logging (not shown to the student)
- `modelUsed` — which model actually ran (mini vs full)
- `totalTokens` / `latencyMs` — cost and performance telemetry

---

## Agent 2 — Code Analysis Agent

### What it does

Reviews a student's submitted code after it has already been evaluated by Judge0. Produces 2–3 structured feedback items covering code quality, optimization opportunities, and a potential AI-generated flag with a confidence score. It is *not* agentic — it runs a fixed pipeline, not a loop.

### Architecture — static workflow

```
Submission evaluation completes (Judge0 verdict written)
    ↓
JudgeEvaluationService calls RunCodeCheckerAgentAsync (background, non-blocking)
    ↓
CodeAnalysisAgentService.AnalyzeAsync:
  1. CodeAnalysisHeuristics.Analyze() — deterministic static scan of the code
  2. Render code-analysis-agent-system.txt with problem + code + heuristic signals
  3. Single LLM call → structured JSON output
  4. Parse + validate feedback items
  5. Apply AI-generated flag if confidence ≥ 0.6
    ↓
Feedback items persisted / returned
```

### Files

| Role | Path |
|---|---|
| Interface + DTOs | `src/Codify.Application/Agents/ICodeCheckerAgent.cs` |
| Implementation | `src/Codify.Infrastructure/AI/CodeAnalysisAgentService.cs` |
| Deterministic heuristics | `src/Codify.Infrastructure/AI/CodeAnalysisHeuristics.cs` |
| System prompt | `src/Codify.Infrastructure/AI/Prompts/code-analysis-agent-system.txt` |
| Trigger site | `src/Codify.Application/Services/JudgeEvaluationService.cs` (step 10) |

### What the heuristics measure

`CodeAnalysisHeuristics` runs before the LLM call and feeds its findings into the prompt as grounding signals. This prevents the model from guessing — it reasons over measured data:

- Total lines, code lines, comment lines, blank lines
- Comment ratio — high ratio is a mild AI signal
- Structured explanation markers: "Approach:", "Time complexity:", "Algorithm:", etc.
- Absence of debug artifacts (prints, TODOs, `// cout`, `# debug`)
- Formatting uniformity (no blank-line separators + deep indentation)
- Descriptor-heavy identifiers (`result`, `current`, `maximum`, `index`, etc.)
- Python triple-quoted docstrings
- Composite `AiLikelihoodHeuristic` score from 0.0 to 1.0

### Feedback types

The LLM is constrained to return only these types:
- `CodeQuality` — style, readability, correctness
- `Optimization` — time/space improvements
- `AiGenerated` — added when `aiGenerated == true` AND `aiGeneratedConfidence >= 0.6`

If the model returns nothing usable but the heuristic score is ≥ 0.6, the agent falls back to a heuristic-only AI-generated flag. If everything fails, it returns a "review temporarily unavailable" fallback item.

### Trigger

The agent is called from `JudgeEvaluationService` at **step 10** — after verdicts are written, counters updated, and the tagging agent has already run. It is awaited directly (not fire-and-forget) but a failure here cannot affect the verdict already persisted.

---

## Agent 3 — Tagging Agent

### What it does

Automatically classifies problems with concept tags (e.g., "Dynamic Programming", "Binary Search", "Hash Map"). It only assigns tags from the database's `ConceptTag` table — it cannot invent new ones. It is *not* agentic — one RAG retrieval, one LLM call, done.

### Architecture — static workflow

```
Trigger (see below)
    ↓
TaggingService checks if problem already has tags → skip if yes
    ↓
TaggingAgentService.ClassifyProblemTagsAsync:
  1. RAG: query Chroma Cloud with problem title + statement → top-3 concept chunks
  2. Render tagging-agent-system.txt with availableTags + retrieved context + problem
  3. Single LLM call → JSON { assignedTags, confidence, reasoning }
  4. Validate: keep only tags that exist in the allowed list (case-insensitive)
  5. Cap at 3 tags
    ↓
TaggingService maps tag names → ConceptTag entities → writes ProblemTag rows → saves
```

### Files

| Role | Path |
|---|---|
| Interface + DTOs | `src/Codify.Application/Agents/ITaggingAgent.cs` |
| Input / Output DTOs | `src/Codify.Application/Agents/ITaggingAgent.cs` (`TaggingAgentInput`, `TagClassificationResult`) |
| Implementation | `src/Codify.Infrastructure/AI/TaggingAgentService.cs` |
| System prompt | `src/Codify.Infrastructure/AI/Prompts/tagging-agent-system.txt` |
| Orchestrator service | `src/Codify.Application/Services/TaggingService.cs` |
| Service interface | `src/Codify.Application/Interfaces/ITaggingService.cs` |
| Response DTOs | `src/Codify.Application/DTOs/AI/TagProblemResponse.cs` |
| API controller | `src/Codify.API/Controllers/AiController.cs` |

### Trigger points

The tagging agent fires from three places:

1. **Manual (instructor only)** — `POST /api/ai/tagging/{problemId}` calls `TaggingService.TagProblemAsync`. Rate-limited. If the problem already has tags it returns immediately without calling the LLM.

2. **On every submission** — `JudgeEvaluationService` calls `TaggingService.TagOnSubmissionAsync` at step 8 (before the code checker at step 10). This:
   - Tags the just-submitted problem if it's untagged
   - Scans and tags **all** currently untagged problems
   - Refreshes the student's weak/strong topic profile

3. **On user progress** — `TaggingService.UpdateUserTagsOnProgressAsync` recomputes the student's concept-tag profile using the existing `PerformanceService`.

### API surface

```
POST /api/ai/tagging/{problemId}   — manual tag (Instructor role, rate-limited)
```

### Output validation

Even if the LLM returns tags with slightly wrong casing or hallucinated names, the service validates every tag against the actual `ConceptTag` database table before persisting. Anything not in the allowed list is silently dropped.

---

## How it all fits together — request flow examples

### Student requests a hint

```
POST /api/hints
  → HintsController
  → AiHintService.GetHintAsync
      loads problem + hint history from DB
      builds TutorAgentInput
      → TutorAgentService.GenerateHintAsync
          sends messages + tool defs to OpenAiChatClient
          model calls tools (0-4 calls across ≤5 iterations)
          TutorAgentTools executes each tool
          model returns final JSON hint
      persists HintLog (with toolsUsed, model, tokens, latency)
      updates performance counters
  → HintResponse returned to client
```

### Student submits code

```
POST /api/submissions
  → SubmissionsController
  → JudgeEvaluationService runs in background
      Judge0 evaluates test cases
      Verdicts written, counters updated
      → TaggingService.TagOnSubmissionAsync   ← Tagging Agent
          tags this problem if untagged
          scans all untagged problems
          refreshes student topic profile
      → CodeAnalysisAgentService.AnalyzeAsync ← Code Analysis Agent
          heuristic scan + single LLM call
          feedback items persisted
```

### Instructor tags a problem manually

```
POST /api/ai/tagging/{problemId}
  → AiController
  → TaggingService.TagProblemAsync
      → TaggingAgentService.ClassifyProblemTagsAsync
          RAG query → Chroma Cloud
          single LLM call → tag classification
          validate against DB tags
      persist ProblemTag rows
  → TagProblemResponse returned
```

---

## No Python, no external agent framework

Everything described above runs entirely inside the .NET 10 backend. There is no Python service, no LangChain, no AutoGen, no separate agent process. The "agentic" behaviour of the Tutor Agent is implemented manually in `TutorAgentService` using OpenAI's native function-calling protocol via the official .NET SDK. The knowledge base uses Chroma Cloud (a hosted vector DB) over plain HTTPS.
