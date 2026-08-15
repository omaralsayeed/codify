using Codify.Domain.Entities;

namespace Codify.Application.Interfaces;

public interface IFeedbackRepository
{
    Task AddRangeAsync(IEnumerable<FeedbackRecord> records);
    Task<IEnumerable<FeedbackRecord>> GetBySubmissionAsync(Guid submissionId);
    Task<List<FeedbackRecord>> GetBySubmissionIdAsync(Guid submissionId);
    Task<List<FeedbackRecord>> GetAiGeneratedFlagsAsync();
    Task SaveChangesAsync();
}
