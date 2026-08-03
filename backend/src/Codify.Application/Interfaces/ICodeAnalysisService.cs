using Codify.Application.DTOs.AI;

namespace Codify.Application.Interfaces;

/// <summary>
/// Orchestrates code analysis: loads problem + test cases, delegates to the
/// Code Analysis Agent, and persists structured feedback as FeedbackRecords.
/// </summary>
public interface ICodeAnalysisService
{
    Task<CodeAnalysisResponse> AnalyzeAsync(CodeAnalysisRequest request, Guid userId, CancellationToken cancellationToken = default);
}
