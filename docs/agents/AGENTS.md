# AGENTS.md
# Codify — AI Agent Design & Contracts

This document reflects the AI implementation that is actually wired in the backend today. The only runtime agent currently active is the Tutor Agent.

## Design Principles

1. Structured output only. The agent returns JSON that the backend can validate.
2. Controlled scope. The Tutor Agent only gives hints and does not provide full solutions.
3. Graceful failure. Model errors return a safe fallback hint.
4. No hallucinated facts. The agent only receives the problem context and student context passed in by the service layer.

## Active Runtime Agent: Tutor Agent

### Trigger

`POST /api/ai/hints`

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

The service maps this request into `TutorAgentInput` and enriches it with the problem title, problem statement, and concept tags.

### Output Contract

```json
{
  "hintText": "Think about which data structure gives you constant-time lookups.",
  "hintLevel": 1,
  "followUpQuestion": "What would you store as the key and value?",
  "hasMoreHints": true
}
```

### Prompt Template

The active prompt lives in [tutor-agent-system.txt](../../backend/src/Codify.Infrastructure/AI/Prompts/tutor-agent-system.txt) and instructs the model to:

- never write the complete solution
- give one hint at a time
- ask a follow-up question
- keep the answer concise
- return valid JSON only

### Validation And Fallback

The backend validates the model response after the call:

- `hintText` must be present
- `hintLevel` must stay within 1 to 3
- invalid JSON returns a fallback hint
- exceptions from the OpenAI client return the same fallback hint

Fallback response:

```json
{
  "hintText": "Try reviewing the problem constraints. They often hint at the right approach.",
  "hintLevel": 1,
  "followUpQuestion": null,
  "hasMoreHints": true
}
```

## Current Gaps

- No code-checker agent is wired into runtime.
- No analytics or tagging agent is wired into runtime.
- No vector retrieval step is wired into the tutor flow yet.
- Hint persistence to `HintLog` is not implemented yet.

## Planned Extensions

If the team resumes the broader multi-agent roadmap, the next additions should be:

1. A retrieval layer for concept context.
2. A feedback agent for submission analysis.
3. A performance-profile updater for student weakness tracking.
4. A proper persistence path for hint history.
