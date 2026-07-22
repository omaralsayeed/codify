using Codify.Domain.Entities;

namespace Codify.Application.Interfaces;

public interface IFeedbackRepository
{
    Task AddRangeAsync(IEnumerable<FeedbackRecord> records);
    Task<List<FeedbackRecord>> GetBySubmissionIdAsync(Guid submissionId);
    Task SaveChangesAsync();
}
