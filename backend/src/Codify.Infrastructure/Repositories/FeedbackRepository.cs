using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Codify.Infrastructure.Repositories;

public class FeedbackRepository(CodifyDbContext db) : IFeedbackRepository
{
    public async Task AddRangeAsync(IEnumerable<FeedbackRecord> records) =>
        await db.FeedbackRecords.AddRangeAsync(records);

    public async Task<IEnumerable<FeedbackRecord>> GetBySubmissionAsync(Guid submissionId) =>
        await db.FeedbackRecords
            .Where(f => f.SubmissionId == submissionId)
            .OrderBy(f => f.CreatedAt)
            .ToListAsync();

    public async Task SaveChangesAsync() =>
        await db.SaveChangesAsync();
}
