using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Codify.Infrastructure.Repositories;

public class PerformanceRepository(CodifyDbContext db) : IPerformanceRepository
{
    public async Task<PerformanceProfile?> GetByUserIdAsync(Guid userId) =>
        await db.PerformanceProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

    public async Task AddAsync(PerformanceProfile profile) =>
        await db.PerformanceProfiles.AddAsync(profile);

    public async Task SaveChangesAsync() =>
        await db.SaveChangesAsync();
}
