using Codify.Domain.Enums;

namespace Codify.Application.Agents;

/// <summary>
/// Input the agent needs to analyze a submission.
/// </summary>
public record CodeCheckerAgentInput(
    Guid   SubmissionId,
    string Code,
    string Language,
    string ProblemTitle,
    string ProblemStatement);

/// <summary>
/// One feedback item returned by the agent.
/// The agent can return multiple items — one per feedback type.
/// </summary>
public record CodeCheckerFeedbackItem(
    FeedbackType FeedbackType,
    string       Message);

/// <summary>
/// Contract for the code checker AI agent.
/// Lives in Application layer — Infrastructure provides the real implementation.
/// </summary>
public interface ICodeCheckerAgent
{
    Task<IReadOnlyList<CodeCheckerFeedbackItem>> AnalyzeAsync(
        CodeCheckerAgentInput input,
        CancellationToken cancellationToken = default);
}
