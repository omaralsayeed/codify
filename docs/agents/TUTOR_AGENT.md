# Tutor Agent

**Type:** Agentic (LLM tool calling) — the only agent that uses open-ended tool calls.
**Trigger:** `POST /api/hints` (Student, rate-limited `ai-hints`).
**Contract:** `ITutorAgent.GenerateHintAsync(TutorAgentInput) -> HintResponse`.

## Purpose
Give progressive, personalized hints without revealing the solution. Ground every hint in
real student data (attempt history, prior hints, partial code) and RAG concept context.

## Flow

```
POST /api/hints
  -> HintsController -> AiHintService.GetHintAsync
       -> compute next hint level from persisted HintLog history
       -> build TutorAgentInput (includes UserId)
  -> TutorAgentService.GenerateHintAsync   (tool-calling loop, max 5 iterations)
       -> model returns tool_calls? execute them, append results, resend
       -> model returns final JSON -> validate + clamp hint level to 1..3
  -> persist HintLog (ResponseText + ToolsUsedJson + ReasoningSummary)
  -> HintResponse
```

## Tools (the model decides which to call)
| Tool | Purpose |
|------|---------|
| `get_attempt_history` | Real attempt count, statuses, previous hint levels, timestamps. |
| `search_knowledge_base` | RAG grounding from Chroma Cloud when a concept is missing. |
| `check_partial_code` | Lightweight static observations (loops, recursion, base case). |
| `get_previous_hints` | Avoid repeating guidance already given. |

No C# code branches on hint level — the model decides what to call and when.

## Output contract
```json
{
  "hintText": "Think about which data structure gives constant-time lookups.",
  "hintLevel": 1,
  "followUpQuestion": "What would you store as the key and value?",
  "hasMoreHints": true,
  "toolsUsed": ["get_attempt_history", "search_knowledge_base"],
  "reasoningSummary": "Student has two WrongAnswer attempts; conceptual nudge first."
}
```
`hintLevel` is the agent's own assessment (clamped 1..3). `toolsUsed` + `reasoningSummary`
are persisted to `HintLog` as evidence of agentic decision-making.

## Guard rails
1. Never reveals the full solution (prompt-enforced).
2. Iteration cap (5) with safe fallback.
3. Structured-output validation; invalid JSON -> fallback hint.
4. Hint level clamped to [1, 3].
5. RAG/tool failures degrade to a hint without that context.

## Key files
- `Codify.Infrastructure/AI/TutorAgentService.cs` — the tool-calling loop.
- `Codify.Infrastructure/AI/TutorAgentTools.cs` + `TutorAgentToolSchemas.cs` — tools + OpenAI schemas.
- `Codify.Application/Agents/ITutorAgent.cs`, `TutorAgentInput.cs`, `ITutorAgentTools.cs`.
- `Codify.Application/Services/AiHintService.cs` — orchestration + persistence.
- Prompt: `Codify.Infrastructure/AI/Prompts/tutor-agent-system.txt`.
