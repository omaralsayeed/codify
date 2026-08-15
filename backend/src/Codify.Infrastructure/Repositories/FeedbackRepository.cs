using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Domain.Enums;
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

    public async Task<List<FeedbackRecord>> GetBySubmissionIdAsync(Guid submissionId) =>
        await db.FeedbackRecords
            .Where(f => f.SubmissionId == submissionId)
            .OrderBy(f => f.CreatedAt)
            .ToListAsync();

    public async Task<List<FeedbackRecord>> GetAiGeneratedFlagsAsync() =>
        await db.FeedbackRecords
            .Include(f => f.Submission)
                .ThenInclude(s => s.User)
            .Include(f => f.Submission)
                .ThenInclude(s => s.Problem)
            .Where(f => f.FeedbackType == FeedbackType.AiGenerated)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

    public async Task SaveChangesAsync() =>
        await db.SaveChangesAsync();
}
