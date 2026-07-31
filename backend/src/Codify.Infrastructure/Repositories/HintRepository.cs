using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Codify.Infrastructure.Repositories;

public class HintRepository(CodifyDbContext db) : IHintRepository
{
    public async Task<int> GetCurrentHintLevelAsync(Guid userId, Guid problemId) =>
        await db.HintLogs
            .Where(h => h.UserId == userId && h.ProblemId == problemId)
            .Select(h => (int?)h.HintLevel)
            .MaxAsync() ?? 0;

    public async Task<IEnumerable<HintLog>> GetByUserAndProblemAsync(Guid userId, Guid problemId) =>
        await db.HintLogs
            .Where(h => h.UserId == userId && h.ProblemId == problemId)
            .OrderBy(h => h.HintLevel)
            .ToListAsync();

    public async Task AddAsync(HintLog hintLog) =>
        await db.HintLogs.AddAsync(hintLog);

    public async Task SaveChangesAsync() =>
        await db.SaveChangesAsync();
}
