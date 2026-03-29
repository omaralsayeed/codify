# AGENTS.md
# Codify — AI Agent Design & Contracts

> This document defines exactly what each agent does, what it receives, what it returns, and the rules it must follow. The AI module in the backend must implement these contracts precisely.

---

## Design Principles (Apply to All Agents)

1. **Structured output only.** Every agent returns JSON — never freeform text that the backend has to parse manually.
2. **Controlled scope.** Each agent does one job. Do not merge Tutor and Code Checker concerns.
3. **Graceful failure.** If the LLM fails or times out, the agent returns a fallback response — it never crashes the request.
4. **No full solutions.** The Tutor Agent must never write the complete working solution, regardless of how the student frames the request.
5. **No hallucinated facts.** Agents must not invent problem constraints, test cases, or student history that wasn't passed in.

---

## Agent 1: AI Tutor Agent

### Purpose
Help the student think through the problem step by step. Give progressively more detailed hints without revealing the full solution.

### When It's Called
POST /api/ai/hints — triggered by the student clicking "Get Hint"

### RAG: Yes
Before calling the LLM, retrieve concept explanations from the vector DB matching the problem's concept tags.

### Input Contract

```json
{
  "problemId": "uuid",
  "problemTitle": "Two Sum",
  "problemStatement": "Given an array of integers nums and an integer target...",
  "conceptTags": ["Arrays & Hashing"],
  "hintLevel": 1,
  "previousHints": [
    "Think about what data structure allows fast lookups."
  ],
  "lastSubmissionStatus": "WrongAnswer",
  "attemptCount": 3,
  "retrievedContext": "A hash map (dictionary) stores key-value pairs and supports O(1) average lookup time..."
}
```

### Output Contract

```json
{
  "hintText": "You're on the right track. Consider storing each number you've seen in a data structure that lets you check in constant time whether the complement of the current number exists.",
  "hintLevel": 1,
  "followUpQuestion": "What would you store as the key, and what as the value?",
  "hasMoreHints": true
}
```

### System Prompt (Template)

```
You are an educational programming tutor. Your job is to help students develop problem-solving skills through guided thinking.

Rules you must follow:
1. NEVER write the complete solution or working code.
2. Give ONE hint at a time, calibrated to the hint level (1 = gentle nudge, 2 = concept-level hint, 3 = structural approach hint).
3. Ask a follow-up question to keep the student thinking.
4. Base your response on the retrieved concept context provided.
5. Be encouraging and concise. Maximum 3 sentences for the hint.
6. Return ONLY valid JSON matching the output schema. No preamble.

Problem: {{problemStatement}}
Concept context: {{retrievedContext}}
Hint level: {{hintLevel}}
Previous hints given: {{previousHints}}
Student attempt count: {{attemptCount}}
```

### Hint Level Guide

| Level | Behavior |
|-------|----------|
| 1 | Vague directional nudge — don't mention the algorithm |
| 2 | Name the relevant concept or data structure |
| 3 | Explain the structural approach without writing code |

### Fallback Response (on LLM failure)
```json
{
  "hintText": "Try reviewing the problem constraints — they often hint at the right approach.",
  "hintLevel": 1,
  "followUpQuestion": null,
  "hasMoreHints": true
}
```

---

## Agent 2: AI Code Checker Agent

### Purpose
After a student submits code, analyze it for quality, logic correctness, optimization opportunities, and basic integrity signals.

### When It's Called
Triggered internally after a submission's execution results are available. The student sees feedback alongside their submission result.

### RAG: No
This agent works directly on the submitted code — no retrieval needed.

### Input Contract

```json
{
  "problemTitle": "Two Sum",
  "problemStatement": "Given an array of integers nums...",
  "problemConstraints": "1 <= nums.length <= 10^4",
  "submittedCode": "def two_sum(nums, target):\n    for i in range(len(nums)):\n        for j in range(i+1, len(nums)):\n            if nums[i] + nums[j] == target:\n                return [i, j]",
  "language": "Python",
  "executionStatus": "Accepted",
  "passedTestCount": 10,
  "totalTestCount": 10,
  "executionTimeMs": 380
}
```

### Output Contract

```json
{
  "verdict": "Accepted with suggestions",
  "codeQualityFeedback": "Your solution is correct. Variable names are clear and the logic is easy to follow.",
  "optimizationSuggestion": "Your current approach runs in O(n²) time. A hash map would reduce this to O(n) by trading space for speed.",
  "integrityFlag": false,
  "integrityNote": null,
  "overallMessage": "Good job solving it! Try to optimize the time complexity as a next challenge."
}
```

### System Prompt (Template)

```
You are a code review assistant for an educational programming platform. Your job is to give constructive, educational feedback on student code submissions.

Rules:
1. Be encouraging. The student just solved a problem.
2. Point out ONE optimization opportunity — do not overwhelm.
3. Flag suspicious patterns only if you are highly confident (exact boilerplate, uncharacteristic complexity for a beginner, etc.).
4. Do NOT rewrite their code. Describe the improvement in words.
5. Return ONLY valid JSON matching the output schema. No preamble.

Problem: {{problemTitle}} — {{problemStatement}}
Constraints: {{problemConstraints}}
Language: {{language}}
Execution result: {{executionStatus}} ({{passedTestCount}}/{{totalTestCount}} tests passed)
Execution time: {{executionTimeMs}}ms

Student code:
{{submittedCode}}
```

### Integrity Flag Guidance
Only set `integrityFlag: true` if the code exhibits clear markers:
- Suspiciously advanced patterns (e.g., complex memoization with decorators from a student with 0 prior correct submissions)
- Identical boilerplate comments in a style inconsistent with educational code
- Do NOT flag O(n) solutions just because they're efficient — students learn

### Fallback Response (on LLM failure)
```json
{
  "verdict": "Analysis unavailable",
  "codeQualityFeedback": "Your submission was evaluated. Manual review may follow.",
  "optimizationSuggestion": null,
  "integrityFlag": false,
  "integrityNote": null,
  "overallMessage": "AI feedback is temporarily unavailable. Your submission result stands."
}
```

---

## Agent 3: Analytics / Tagging Agent

### Purpose
Two jobs:
1. **Problem tagging** — assign concept tags to a problem when it's created (or on demand by instructor)
2. **Performance analysis** — after each submission, update the student's weakness/strength profile and generate next-step recommendations

### When It's Called
- Automatically after each submission is evaluated
- On-demand when an instructor creates a new problem without tags

### RAG: Yes (for recommendations)
Retrieve related problems from the vector DB to suggest next exercises.

### Input Contract (Submission Analysis)

```json
{
  "userId": "uuid",
  "problemId": "uuid",
  "conceptTags": ["Dynamic Programming"],
  "submissionStatus": "WrongAnswer",
  "attemptCount": 4,
  "hintCount": 2,
  "currentProfile": {
    "successRate": 0.55,
    "weakTopics": ["Dynamic Programming"],
    "strongTopics": ["Arrays & Hashing"]
  }
}
```

### Output Contract (Submission Analysis)

```json
{
  "updatedWeakTopics": ["Dynamic Programming", "Graphs"],
  "updatedStrongTopics": ["Arrays & Hashing"],
  "updatedSuccessRate": 0.52,
  "recommendation": {
    "message": "You're struggling with Dynamic Programming. Try starting with simpler memoization problems.",
    "suggestedConceptTag": "Dynamic Programming"
  }
}
```

### Input Contract (Problem Tagging)

```json
{
  "problemTitle": "Coin Change",
  "problemStatement": "You are given an integer array coins representing coin denominations...",
  "availableTags": ["Arrays & Hashing", "Dynamic Programming", "Greedy", "Graphs", "Recursion"]
}
```

### Output Contract (Problem Tagging)

```json
{
  "assignedTags": ["Dynamic Programming"],
  "confidence": 0.92,
  "reasoning": "The problem requires finding the minimum number of coins, which is a classic unbounded knapsack variant solved with bottom-up DP."
}
```

### Fallback Response (on LLM failure)
- For submission analysis: skip profile update, log the failure, retry on next submission
- For problem tagging: return empty tags array, flag for manual tagging by instructor

---

## RAG Pipeline Design

### What Gets Indexed
- Concept tag descriptions (from `ConceptTags.Description`)
- Problem statements (from `Problems.Statement`)
- Curated concept explanations (short educational paragraphs per concept)

### Retrieval Strategy
- Query: problem title + concept tags
- Top-k: 3 chunks
- Embed using the same provider as the LLM (OpenAI `text-embedding-3-small` or Gemini equivalent)

### When to Re-index
- When a new problem is added
- When a concept tag description is updated
- Initial seed: run a one-time indexing job at project setup

---

## Agent Orchestration Flow

```
HTTP Request
     │
     ▼
Controller
     │
     ▼
AgentOrchestrator (Application Layer)
     │
     ├── Builds input context from DB
     │
     ├── (if RAG) → VectorDBClient.Query()
     │                      │
     │              Retrieved chunks
     │
     ├── Merges context + retrieved chunks
     │
     ├── LLMClient.Complete(systemPrompt, userPayload)
     │
     ├── Parses JSON response
     │
     ├── Validates against output schema
     │
     └── Returns typed result to service layer
```

---

## Output Validation Rules

Before returning agent output to the caller, the backend must validate:
- `hintText` is not empty
- `hintLevel` is 1, 2, or 3
- `integrityFlag` is a boolean
- `assignedTags` are all valid ConceptTag names
- If JSON parse fails → use fallback response + log error
