using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Codify.Infrastructure.Repositories;

/// <summary>
/// Repository for the PerformanceProfile entity (1:1 with User).
/// Used by the Analytics Agent to upsert the computed learning profile.
/// </summary>
public class PerformanceProfileRepository(CodifyDbContext db) : IPerformanceProfileRepository
{
    public async Task<PerformanceProfile?> GetByUserIdAsync(Guid userId) =>
        await db.PerformanceProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

    public async Task AddAsync(PerformanceProfile profile) =>
        await db.PerformanceProfiles.AddAsync(profile);

    public async Task SaveChangesAsync() =>
        await db.SaveChangesAsync();
}
