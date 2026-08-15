using Codify.Application.DTOs.AI;

namespace Codify.Application.Interfaces;

/// <summary>
/// Orchestrates the Tagging Agent (a static workflow, fired by events). It:
///  - tags a single untagged problem (RAG-grounded classification),
///  - runs an automatic scan that tags all currently-untagged problems, and
///  - refreshes a user's concept-tag profile whenever they make progress.
/// </summary>
public interface ITaggingService
{
    /// <summary>Classifies and applies concept tags to one problem if it is untagged.</summary>
    Task<TagProblemResponse> TagProblemAsync(Guid problemId, CancellationToken cancellationToken = default);

    /// <summary>Scans all untagged problems and tags each one. Returns the scan summary.</summary>
    Task<TagScanResponse> TagAllUntaggedProblemsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fired on every problem submission (after evaluation). Tags the just-submitted
    /// problem if untagged, scans and tags all other untagged problems, and refreshes
    /// the student's weak/strong topic profile.
    /// </summary>
    Task TagOnSubmissionAsync(Guid problemId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fired on student progress to refresh the user's weak/strong concept-tag profile.
    /// </summary>
    Task UpdateUserTagsOnProgressAsync(Guid userId, CancellationToken cancellationToken = default);
}
