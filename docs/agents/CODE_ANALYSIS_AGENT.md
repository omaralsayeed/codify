# Code Analysis Agent

**Type:** Static workflow (fixed pipeline, single LLM call). Not agentic.
**Trigger:** Fired automatically after a submission is evaluated (background pipeline).
**Contract:** `ICodeCheckerAgent.AnalyzeAsync(CodeCheckerAgentInput) -> IReadOnlyList<CodeCheckerFeedbackItem>`.

## Purpose
Review an accepted/evaluated submission and produce constructive feedback. Also assess
whether the code appears to be **AI-generated**.

## Flow (fixed pipeline)

```
submission evaluated (JudgeEvaluationService, background)
  -> CodeAnalysisAgentService.AnalyzeAsync
       1. CodeAnalysisHeuristics.Analyze(code, language)   [deterministic signals]
       2. render code-analysis-agent-system.txt with code + heuristics
       3. single ILLMClient.CompleteAsync call
       4. parse JSON -> feedback items + AI-generated verdict
       5. map to FeedbackRecord rows (FeedbackType.CodeQuality/Optimization/IntegrityFlag/AiGenerated)
```

The deterministic heuristics (line counts, comment ratio, indent depth, AI-style signals)
ground the LLM so it reasons over measured evidence instead of guessing.

## AI-generated detection
- The LLM returns `aiGenerated` (bool), `aiGeneratedConfidence` (0..1), and indicators.
- A `FeedbackType.AiGenerated` record is emitted only when confidence >= **0.6**.
- If the model returns nothing usable, the deterministic heuristic score is used as a
  fallback signal.

## Guard rails
1. Structured output validation; unknown feedback types are skipped.
2. LLM failure or invalid JSON -> safe fallback feedback item.
3. Runs off the request thread; a failure never affects the evaluated submission.
4. AI-detection is a best-effort signal, gated by a confidence threshold.

## Key files
- `Codify.Infrastructure/AI/CodeAnalysisAgentService.cs` — the static workflow.
- `Codify.Infrastructure/AI/CodeAnalysisHeuristics.cs` — deterministic code signals.
- `Codify.Application/Agents/ICodeCheckerAgent.cs` — contract (input + feedback item).
- `Codify.Domain/Enums/FeedbackType.cs` — includes `AiGenerated`.
- Prompt: `Codify.Infrastructure/AI/Prompts/code-analysis-agent-system.txt`.
- Wired in `Codify.Application/Services/JudgeEvaluationService.cs`.
