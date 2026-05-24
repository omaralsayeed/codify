using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Codify.Infrastructure.Repositories;

public class SubmissionRepository(CodifyDbContext db) : ISubmissionRepository
{
    public async Task<IEnumerable<Submission>> GetByProblemAndUserAsync(Guid problemId, Guid? userId)
    {
        var query = db.Submissions
            .Where(s => s.ProblemId == problemId)
            .AsQueryable();

        if (userId.HasValue)
            query = query.Where(s => s.UserId == userId.Value);

        return await query
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();
    }

    public async Task<Submission?> GetByIdWithDetailsAsync(Guid id) =>
        await db.Submissions
            .Include(s => s.Result)
            .Include(s => s.FeedbackRecords)
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task<IEnumerable<Submission>> GetAllByUserAsync(Guid userId) =>
        await db.Submissions
            .Include(s => s.Problem)
                .ThenInclude(p => p.ProblemTags)
                    .ThenInclude(pt => pt.ConceptTag)
            .Where(s => s.UserId == userId)
            .ToListAsync();

    public async Task AddAsync(Submission submission) =>
        await db.Submissions.AddAsync(submission);

    public async Task SaveChangesAsync() =>
        await db.SaveChangesAsync();
}
