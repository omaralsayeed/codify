# Tutor Agent

The Tutor Agent helps students solve programming problems through their own reasoning. It is implemented as an **agentic** system: the LLM decides which tools to call, in what order, and how many, before producing a hint. The C# backend does not hardcode this decision logic.

## Purpose

- Provide progressive, personalized hints without revealing the full solution.
- Ground hints in real student data: attempt history, previous hints, partial code, and concept docs.
- Produce structured JSON that the backend validates and persists.
- Leave auditable evidence of the agent's decisions (`tools_used`, `reasoning_summary`) in `HintLog`.

## When It Runs

`POST /api/ai/hints` — a student requests a hint for a problem.

## High-Level Sequence

```text
Student
  -> POST /api/ai/hints
  -> AiController
  -> AiHintService.GetHintAsync
       -> validate HintRequest
       -> load problem + concept tags from IProblemRepository
       -> build TutorAgentInput (includes UserId)
  -> TutorAgentService.GenerateHintAsync
       -> load tutor-agent-system.txt via IPromptLoader
       -> send system prompt + user message + 4 tool definitions to OpenAI
       -> OpenAI tool-calling loop (max 5 iterations)
            -> model returns tool_calls
            -> TutorAgentTools executes each tool
            -> append results to conversation
            -> resend
       -> model returns final JSON
       -> parse and validate
  -> AiHintService persists HintLog with ToolsUsedJson + ReasoningSummary
  -> ApiResponse.Ok(HintResponse)
  -> Student
```

## Tools (OpenAI Function Calling)

The agent has four tools. The **model** chooses which to use.

| # | Tool | Input | Output | Why the Agent Calls It |
|---|------|-------|--------|------------------------|
| 1 | `get_attempt_history` | `studentId`, `problemId` | attempt count, submission statuses, previous hint levels, timestamps | To judge how stuck the student really is, instead of trusting the client-supplied hint level. |
| 2 | `search_knowledge_base` | `query`, optional `conceptTag` | list of relevant concept-doc snippets | To retrieve grounding material when the student seems to be missing an underlying concept. |
| 3 | `check_partial_code` | `code`, `language` | syntax validity + structural observations (loops, recursion, base case) | To tailor the hint to the code the student actually wrote. |
| 4 | `get_previous_hints` | `studentId`, `problemId` | exact text of prior hints | To avoid repeating guidance and judge how much more specific to get. |

No C# code contains `if (hintLevel == 1) callToolX()` — the model decides at inference time.

## LangGraph / Agent Loop

Although the backend is C#, the loop mirrors a LangGraph-style tool-calling agent:

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
   | Tools       |  | JSON        |
   +------+------+  +------+------+
          |                |
          +----------------+
          |
          v
   +------+------+
   |   Resend    |
   +-------------+
```

- The loop caps at **5 iterations** to control cost and latency.
- If the cap is hit, the service logs a warning and returns a fallback hint.
- If the LLM call throws, the service returns a fallback hint.

## Input Contract

`HintRequest` from the client:

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

`TutorAgentInput` (enriched by `AiHintService`):

- `UserId` — to query real history and previous hints
- `ProblemId`
- `ProblemTitle`
- `ProblemStatement`
- `ConceptTags`
- `HintLevel` (suggested)
- `StudentCode`
- `Language`
- `LastSubmissionStatus`

## Output Contract

```json
{
  "hintText": "Think about which data structure gives you constant-time lookups.",
  "hintLevel": 1,
  "followUpQuestion": "What would you store as the key and value?",
  "hasMoreHints": true,
  "toolsUsed": ["get_attempt_history"],
  "reasoningSummary": "Student has two WrongAnswer attempts; conceptual nudge is appropriate first."
}
```

`hintLevel` is the **agent's own assessment** (1–3), not the client-supplied value.

## Guard Rails

1. **No full solutions.** The system prompt explicitly forbids working code or complete answers.
2. **No hardcoded tool logic.** The C# loop executes whatever tool calls the model returns.
3. **Hint level clamping.** The backend clamps `hintLevel` to [1, 3].
4. **Iteration cap.** Max 5 tool-calling rounds; fallback on exhaustion.
5. **Structured output validation.** Invalid JSON or missing `hintText` returns a fallback.
6. **Persistence evidence.** Every hint stores `ToolsUsedJson` + `ReasoningSummary` in `HintLog`.

## Key Files

| Layer | File | Responsibility |
|-------|------|----------------|
| Application interface | `backend/src/Codify.Application/Agents/ITutorAgent.cs` | Contract for the agent. |
| Application input | `backend/src/Codify.Application/Agents/TutorAgentInput.cs` | Strongly typed input. |
| Application tools | `backend/src/Codify.Application/Interfaces/ITutorAgentTools.cs` | Tool interface + result DTOs. |
| Application LLM | `backend/src/Codify.Application/Interfaces/ILLMClient.cs` | `CompleteWithToolsAsync` primitive. |
| Application service | `backend/src/Codify.Application/Services/AiHintService.cs` | Orchestrates request -> agent -> persistence. |
| Domain | `backend/src/Codify.Domain/Entities/HintLog.cs` | Entity extended with `ToolsUsedJson` and `ReasoningSummary`. |
| Infrastructure agent | `backend/src/Codify.Infrastructure/AI/TutorAgentService.cs` | Tool-calling loop. |
| Infrastructure tools | `backend/src/Codify.Infrastructure/AI/TutorAgentTools.cs` | Tool implementations. |
| Infrastructure schemas | `backend/src/Codify.Infrastructure/AI/TutorAgentToolSchemas.cs` | OpenAI function schemas + dispatch. |
| Infrastructure LLM | `backend/src/Codify.Infrastructure/AI/OpenAiChatClient.cs` | OpenAI SDK tool-calling implementation. |
| Infrastructure prompt | `backend/src/Codify.Infrastructure/AI/Prompts/tutor-agent-system.txt` | Agentic system prompt. |
| Infrastructure repos | `backend/src/Codify.Infrastructure/Repositories/HintLogRepository.cs` | Reads/writes hint history. |
| Infrastructure search | `backend/src/Codify.Infrastructure/Search/KnowledgeBaseSearchService.cs` | Searches `ConceptTag` descriptions. |
| API | `backend/src/Codify.API/Controllers/AiController.cs` | `POST /api/ai/hints`. |

## Testing

Unit tests live in `backend/tests/Codify.Tests/Infrastructure/`:

- `TutorAgentToolsTests.cs` — verifies each tool returns correct deterministic data.
- `TutorAgentServiceTests.cs` — verifies the loop executes model-requested tools, terminates on final text, caps iterations, and clamps hint levels.
