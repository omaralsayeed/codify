# Analytics / Tagging Agent

The Analytics / Tagging Agent builds a learning profile for each student. It is implemented as an **agentic** system: the LLM decides which tools to call, in what order, and how many, before producing the analytics JSON. The C# backend does not hardcode this decision logic.

## Purpose

- Analyze submission history to identify weak topics, strong topics, and learning trends.
- Generate structured learning tags and personalized recommendations.
- Tag untagged problems by finding similar already-tagged problems in the vector store.
- Ground recommendations in retrieved concept descriptions from the Chroma knowledge base.

## When It Runs

- `POST /api/ai/analytics` — a student generates/refreshes their analytics profile.
- `GET /api/ai/analytics` — a student reads their stored profile.
- (Internal) `classify_problem_tags` — used when an untagged problem needs ConceptTag suggestions.

## High-Level Sequence

```text
Student
  -> POST /api/ai/analytics
  -> AiController
  -> AnalyticsService.AnalyzeAsync
       -> load all submissions for user via ISubmissionRepository
       -> build AnalyticsAgentInput
  -> AnalyticsAgentService.AnalyzeAsync
       -> load analytics-agent-system.txt via IPromptLoader
       -> send system prompt + user message + tool definitions to OpenAI
       -> OpenAI tool-calling loop (max 5 iterations)
            -> model returns tool_calls
            -> AnalyticsAgentTools executes each tool
            -> append results to conversation
            -> resend
       -> model returns final JSON
       -> parse and validate
  -> AnalyticsService upserts PerformanceProfile
  -> ApiResponse.Ok(AnalyticsResponse)
  -> Student
```

## Tools (OpenAI Function Calling)

The agent has seven tools. The **model** chooses which to use.


## RAG Usage

### Recommendation Grounding

When the agent recommends a weak topic, it can call `get_concept_context`. This tool:

1. Embeds the topic name via `text-embedding-3-small`.
2. Queries the Chroma vector store for `source: "concept"` documents.
3. Returns the top-k similar chunks.
4. The LLM uses these chunks to write a contextualized recommendation instead of generic advice.

### Concept Classification

When tagging an untagged problem, the agent can call `classify_problem_tags`. This tool:

1. Embeds the problem title + statement.
2. Queries Chroma for `source: "problem"` documents.
3. Aggregates the `concept_tag` metadata of the nearest neighbors by vote.
4. Returns the top-3 tag suggestions with confidence scores.

## LangGraph / Agent Loop

```text
        +------------------+
        |  Load Prompts    |
        +--------+---------+
                 |
                 v
        +--------+---------+
        |  Send to LLM     |
        +--------+---------+
                 |
        +---------+----------+
        | Has tool_calls?    |
        +---------+----------+
           yes |        | no
               v        v
    +----------+--+  +--+----------+
    | Execute     |  | Parse Final |

## Input Contract

`AnalyticsAgentInput` (built by `AnalyticsService`):

- `UserId`
- `AvailableTopics` — all ConceptTag names in the platform
- `HintCount` — number of prior hints requested

## Output Contract

```json
{
  "userId": "uuid",
  "learningStage": "Intermediate",
  "overallScore": 72,
  "consistency": "High",
  "confidence": 0.85,
  "weakTopics": ["Dynamic Programming", "Graphs"],
  "strongTopics": ["Arrays & Hashing", "Sorting"],
  "improvingTopics": ["Binary Search"],
  "decliningTopics": ["Trees"],
  "commonMistakes": ["Recurring compilation errors"],
  "recommendedTopics": ["Memoization", "Shortest Path"],
  "recommendedProblemDifficulty": "Medium",
  "practicePlan": [
    { "topic": "Dynamic Programming", "action": "Review memoization patterns", "priority": 1 }
  ],
  "summary": "The student shows strong fundamentals...",
  "toolsUsed": ["get_submission_history", "aggregate_topic_performance"],
  "reasoningSummary": "Calculated topic stats to identify weak areas."
}
```

## Guard Rails

1. **Deterministic-first.** Topic stats, success rates, and trends are computed from real submission data without LLM inference.
2. **No hardcoded tool logic.** The C# loop executes whatever tool calls the model returns.
3. **Iteration cap.** Max 5 tool-calling rounds; fallback on exhaustion.
4. **Structured output validation.** Invalid JSON returns a fallback profile.
5. **RAG grounding.** Recommendations can be grounded in retrieved concept chunks from Chroma.
6. **Problem tagging by similarity.** Untagged problems are classified against real existing tagged problems in the vector store.

    | Tools       |  | JSON        |
    +------+------+  +------+------+
           |                |
           +----------------+
           |
           v
    +------+------+

## Key Files

| Layer | File | Responsibility |
|-------|------|----------------|
| Application interface | `backend/src/Codify.Application/Agents/IAnalyticsAgent.cs` | Contract for the agent. |
| Application tools | `backend/src/Codify.Application/Interfaces/IAnalyticsAgentTools.cs` | Tool interface + result DTOs. |
| Application LLM | `backend/src/Codify.Application/Interfaces/ILLMClient.cs` | `CompleteWithToolsAsync` primitive. |
| Application service | `backend/src/Codify.Application/Services/AnalyticsService.cs` | Orchestrates request -> agent -> persistence. |
| Domain | `backend/src/Codify.Domain/Entities/PerformanceProfile.cs` | Stores analytics profile. |
| Infrastructure agent | `backend/src/Codify.Infrastructure/AI/AnalyticsAgentService.cs` | Tool-calling loop. |
| Infrastructure tools | `backend/src/Codify.Infrastructure/AI/AnalyticsAgentTools.cs` | Tool implementations, including RAG tools. |
| Infrastructure schemas | `backend/src/Codify.Infrastructure/AI/AnalyticsAgentToolSchemas.cs` | OpenAI function schemas + dispatch. |
| Infrastructure LLM | `backend/src/Codify.Infrastructure/AI/OpenAiChatClient.cs` | OpenAI SDK tool-calling implementation. |
| Infrastructure prompt | `backend/src/Codify.Infrastructure/AI/Prompts/analytics-agent-system.txt` | Agentic system prompt. |
| Infrastructure vector | `backend/src/Codify.Infrastructure/AI/ChromaVectorStore.cs` | Chroma HTTP client. |
| Infrastructure embeddings | `backend/src/Codify.Infrastructure/AI/OpenAiEmbeddingService.cs` | OpenAI embeddings client. |
| Infrastructure ingestion | `backend/src/Codify.Infrastructure/Search/ConceptDocumentIngestionService.cs` | Chunks/upserts concept docs and problems. |
| API | `backend/src/Codify.API/Controllers/AiController.cs` | Analytics endpoints. |

## Testing

Unit tests live in `backend/tests/Codify.Tests/Infrastructure/`:

- `AnalyticsAgentToolsRagTests.cs` — verifies RAG tools embed queries and return retrieved data.
- `KnowledgeBaseSearchServiceTests.cs` — verifies vector search path for the Tutor Agent.

    |   Resend    |
    +-------------+
```

- The loop caps at **5 iterations** to control cost and latency.
- If the cap is hit, a fallback profile is returned and a warning is logged.

| # | Tool | Input | Output | Why the Agent Calls It |
|---|------|-------|--------|------------------------|
| 1 | `get_submission_history` | `userId` | all submissions with topics, statuses, timestamps | Starting point for any analytics task. |
| 2 | `aggregate_topic_performance` | `userId` | per-topic solved/failed, success rate, avg attempts | To rank strong vs weak topics. |
| 3 | `detect_weaknesses` | `userId` | common mistakes and weak topics | To find recurring error patterns. |
| 4 | `analyze_trends` | `userId` | improving/declining topics, consistency, velocity | To detect change over time. |
| 5 | `generate_tags` | `userId` | learning stage, score, confidence, weak/strong topics | To produce structured learning tags. |
| 6 | `get_concept_context` | `topic` | retrieved concept chunks from **Chroma** | To ground recommendations with real concept explanations. |
| 7 | `classify_problem_tags` | `problemTitle`, `problemStatement` | suggested ConceptTags | To tag untagged problems via vector similarity to existing tagged problems. |

No C# code contains `if (weakTopic == "X") callToolY()` — the model decides at inference time.
