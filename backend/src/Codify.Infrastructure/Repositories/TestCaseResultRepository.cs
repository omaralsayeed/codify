using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Codify.Infrastructure.Repositories;

public class TestCaseResultRepository(CodifyDbContext db) : ITestCaseResultRepository
{
    public async Task AddRangeAsync(IEnumerable<TestCaseResult> results) =>
        await db.TestCaseResults.AddRangeAsync(results);

    public async Task<List<TestCaseResult>> GetBySubmissionIdAsync(Guid submissionId) =>
        await db.TestCaseResults
            .Where(r => r.SubmissionId == submissionId)
            .OrderBy(r => r.OrderIndex)
            .ToListAsync();

    public async Task SaveChangesAsync() =>
        await db.SaveChangesAsync();
}
