using Codify.Application.Agents;
using Codify.Application.DTOs.AI;
using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Domain.Enums;
using Codify.Domain.Exceptions;

namespace Codify.Application.Services;

/// <summary>
/// Orchestrates code analysis: loads the problem + test cases, delegates to
/// the .NET-native Code Analysis Agent, and persists structured feedback as
/// FeedbackRecords on the submission.
/// </summary>
public class CodeAnalysisService(
    IProblemRepository problemRepo,
    ISubmissionRepository submissionRepo,
    ICodeAnalysisAgent analysisAgent) : ICodeAnalysisService
{
    public async Task<CodeAnalysisResponse> AnalyzeAsync(
        CodeAnalysisRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var problem = await problemRepo.GetByIdWithTestCasesAsync(request.ProblemId)
            ?? throw new NotFoundException($"Problem {request.ProblemId} not found.");

        // Resolve code: from the request, or from an existing submission.
        string code = request.Code;
        Guid? submissionId = request.SubmissionId;
        if (submissionId.HasValue)
        {
            var submission = await submissionRepo.GetByIdWithDetailsAsync(submissionId.Value)
                ?? throw new NotFoundException($"Submission {submissionId} not found.");
            code = submission.Code;
        }

        var input = new CodeAnalysisAgentInput
        {
            ProblemId = problem.Id,
            Code = code,
            Language = request.Language,
            ProblemTitle = problem.Title,
            ProblemStatement = problem.Statement,
            Constraints = problem.Constraints,
            Difficulty = problem.Difficulty.ToString(),
            TimeLimitMs = problem.TimeLimitMs,
            MemoryLimitMb = problem.MemoryLimitMb,
            TestCases = problem.TestCases
                .OrderBy(tc => tc.OrderIndex)
                .Select(tc => new TestCasePayload
                {
                    InputData = tc.InputData,
                    ExpectedOutput = tc.ExpectedOutput,
                    IsSample = tc.IsSample,
                    OrderIndex = tc.OrderIndex
                }).ToList(),
            UserId = userId,
            SubmissionId = submissionId
        };

        var result = await analysisAgent.AnalyzeAsync(input, cancellationToken);

        // Persist structured feedback if we have a submission to attach it to.
        if (submissionId.HasValue)
        {
            var submission = await submissionRepo.GetByIdWithDetailsAsync(submissionId.Value);
            if (submission is not null)
            {
                if (!string.IsNullOrWhiteSpace(result.CodeQualityFeedback))
                    submission.FeedbackRecords.Add(FeedbackRecord.Create(
                        submissionId.Value, FeedbackType.CodeQuality, result.CodeQualityFeedback));
                if (!string.IsNullOrWhiteSpace(result.OptimizationSuggestion))
                    submission.FeedbackRecords.Add(FeedbackRecord.Create(
                        submissionId.Value, FeedbackType.Optimization, result.OptimizationSuggestion));
                if (result.IntegrityFlag)
                    submission.FeedbackRecords.Add(FeedbackRecord.Create(
                        submissionId.Value, FeedbackType.IntegrityFlag,
                        result.IntegrityNote ?? "Integrity flag raised by Code Analysis Agent."));
                await submissionRepo.SaveChangesAsync();
            }
        }

        return MapToResponse(result);
    }

    private static CodeAnalysisResponse MapToResponse(CodeAnalysisResult r) => new()
    {
        Verdict = r.Verdict,
        CodeQualityFeedback = r.CodeQualityFeedback,
        OptimizationSuggestion = r.OptimizationSuggestion,
        TimeComplexity = r.TimeComplexity,
        SpaceComplexity = r.SpaceComplexity,
        IntegrityFlag = r.IntegrityFlag,
        IntegrityNote = r.IntegrityNote,
        OverallMessage = r.OverallMessage,
        TestResults = r.TestResults.Select(t => new AnalysisTestResult
        {
            Input = t.InputData,
            ExpectedOutput = t.ExpectedOutput,
            ActualOutput = t.ActualOutput,
            Passed = t.Passed,
            Stderr = t.Stderr,
            ExecutionTimeMs = t.ExecutionTimeMs
        }).ToList(),
        StaticFindings = r.StaticFindings.Select(f => new AnalysisStaticFinding
        {
            RuleId = f.RuleId,
            Severity = f.Severity,
            Line = f.Line,
            Message = f.Message
        }).ToList(),
        ToolsUsed = r.ToolsUsed,
        ReasoningSummary = r.ReasoningSummary
    };
}
