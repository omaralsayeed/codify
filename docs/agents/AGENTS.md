# Codify — AI Agent Design & Contracts

This document reflects the AI implementation that is actually wired in the backend today. All three agents — **Tutor Agent**, **Code Analysis Agent**, and **Analytics / Tagging Agent** — are active. Each one is an agentic .NET service that uses OpenAI function calling to decide for itself which tools to call before producing a response.

The Python sidecar described in earlier roadmap documents has been removed; the Tutor Agent was rewritten in C# with native tool use, and the Code Analysis / Analytics agents merged from the backup were converted to the same .NET-native agentic architecture.

## Design Principles

1. **Agentic decision-making.** The model decides which tools to call, in what order, and how many — our C# code only executes the tool calls the model requests.
2. **Structured output only.** The agent returns JSON that the backend can validate.
3. **Controlled scope.** The Tutor Agent only gives hints and does not provide full solutions.
4. **Graceful failure.** Model errors, invalid JSON, or hitting the iteration cap return a safe fallback hint.
5. **Evidence of reasoning.** `tools_used` and `reasoning_summary` are persisted to `HintLog` so the agent's decision-making can be inspected.
6. **No hallucinated facts.** Tools ground the agent in real attempt history, previous hints, concept docs, and the student's actual code.

## Active Runtime Agents

| Agent | Trigger | Role |
|-------|---------|------|
| **Tutor Agent** | `POST /api/ai/hints` | Provides progressive, personalized hints without giving the solution. |
| **Code Analysis Agent** | `POST /api/ai/analyze` | Analyzes submitted code using sandboxed execution, static analysis, and complexity estimation. |
| **Analytics / Tagging Agent** | `POST /api/ai/analytics` | Builds a learning profile from submission history, identifying weak/strong topics and generating recommendations. |

## Tutor Agent

### Input Contract

```json
{
  "problemId": "uuid",
  "studentCode": "def solution():\n    ...",
  "hintLevel": 1,
  "previousHints": ["Try thinking about lookups."],
  "attemptCount": 2,
  "lastSubmissionStatus": "WrongAnswer"
}
```

The service maps this request into `TutorAgentInput` (now including `UserId`) and enriches it with the problem title, problem statement, and concept tags. `hintLevel` is treated as a **suggestion**; the agent determines the actual level.

### Output Contract

```json
{
  "hintText": "Think about which data structure gives you constant-time lookups.",
  "hintLevel": 1,
  "followUpQuestion": "What would you store as the key and value?",
  "hasMoreHints": true,
  "toolsUsed": ["get_attempt_history", "search_knowledge_base"],
  "reasoningSummary": "Student has two WrongAnswer attempts and seems to be missing the concept of hash-based lookups."
}
```

### Agent Loop

```text
POST /api/ai/hints
  -> AiController
  -> AiHintService.GetHintAsync
  -> validate hint level (suggestion only)
  -> load problem with tags + user id
  -> build TutorAgentInput
  -> TutorAgentService.GenerateHintAsync
       -> load tutor-agent-system.txt
       -> send system prompt + user message + 4 tool definitions to OpenAI
       -> while model returns tool_calls and iterations < 5:
            -> execute each requested tool
            -> append tool results to messages
            -> resend to model
       -> model returns final JSON
       -> parse and validate
  -> persist HintLog with tools_used + reasoning_summary
  -> return HintResponse
```

### Available Tools

| Tool | Purpose |
|------|---------|
| `get_attempt_history` | How stuck is the student? Returns attempt count, statuses, previous hint levels, timestamps. |
| `search_knowledge_base` | Retrieves concept-level grounding from **Chroma vector store** when the student is missing an underlying idea. Embeds the query and returns top-k similar chunks. |
| `check_partial_code` | Lightweight static observations on the student's code (loops, recursion, base case, syntax heuristics). |
| `get_previous_hints` | Avoids repeating guidance and judges how much more specific the next hint should be. |

The **model** decides which of these tools to call, if any, and in what order. The C# code does not hardcode this logic.

### System Prompt

The active prompt lives in [tutor-agent-system.txt](../../backend/src/Codify.Infrastructure/AI/Prompts/tutor-agent-system.txt). It instructs the model to:

- decide which tools it needs before responding
- never write the complete solution
- prefer guiding questions and conceptual nudges
- escalate specificity gradually based on attempt history
- ground concept explanations in retrieved knowledge-base content
- return valid structured JSON

### Validation And Fallback

The backend validates the model response after the loop:

- `hintText` must be present
- `hintLevel` is clamped to 1 to 3
- invalid JSON or exceptions return a fallback hint
- hitting the 5-iteration cap returns a fallback hint and logs a warning

Fallback response:

```json
{
  "hintText": "Try reviewing the problem constraints. They often hint at the right approach.",
  "hintLevel": 1,
  "followUpQuestion": null,
  "hasMoreHints": true,
  "toolsUsed": [],
  "reasoningSummary": "Fallback: LLM call failed or returned invalid response."
}
```

### Persistence

Every hint is persisted to `HintLog` with:

- `HintLevel` (the agent's own assessment)
- `RequestText`
- `ResponseText`
- `ToolsUsedJson`
- `ReasoningSummary`
- `CreatedAt`

This closes the previously documented gap and provides the evidence the instructor needs to see agentic decision-making.

## Current Gaps

- No runtime event automatically triggers analytics refresh after every submission (could be added as a fire-and-forget call in `SubmissionService`).

## Future Extensions

1. Auto-refresh analytics after each accepted submission.
2. Add an instructor dashboard endpoint using `GET /api/ai/analytics/{userId}`.
3. Let the Tutor Agent consume `PerformanceProfile.WeakTopicsJson` to personalize hints further.
