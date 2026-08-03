using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Codify.Infrastructure.Repositories;

public class HintLogRepository(CodifyDbContext db) : IHintLogRepository
{
    public async Task<IEnumerable<HintLog>> GetByUserAndProblemAsync(Guid userId, Guid problemId) =>
        await db.HintLogs
            .Where(h => h.UserId == userId && h.ProblemId == problemId)
            .OrderBy(h => h.CreatedAt)
            .ToListAsync();

    public async Task AddAsync(HintLog hintLog) =>
        await db.HintLogs.AddAsync(hintLog);

    public async Task SaveChangesAsync() =>
        await db.SaveChangesAsync();
}
