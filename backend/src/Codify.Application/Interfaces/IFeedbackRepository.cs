using Codify.Domain.Entities;

namespace Codify.Application.Interfaces;

public interface IFeedbackRepository
{
    Task AddRangeAsync(IEnumerable<FeedbackRecord> records);
    Task<IEnumerable<FeedbackRecord>> GetBySubmissionAsync(Guid submissionId);
    Task SaveChangesAsync();
}
