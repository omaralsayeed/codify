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
}
