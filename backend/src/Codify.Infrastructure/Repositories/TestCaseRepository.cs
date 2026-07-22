using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Codify.Infrastructure.Repositories;

public class TestCaseRepository(CodifyDbContext db) : ITestCaseRepository
{
    public async Task<List<TestCase>> GetByProblemIdAsync(Guid problemId) =>
        await db.TestCases
            .Where(tc => tc.ProblemId == problemId)
            .OrderBy(tc => tc.OrderIndex)
            .ToListAsync();

    public async Task<TestCase?> GetByIdAsync(Guid id) =>
        await db.TestCases.FirstOrDefaultAsync(tc => tc.Id == id);

    public async Task AddAsync(TestCase testCase) =>
        await db.TestCases.AddAsync(testCase);

    public async Task SaveChangesAsync() =>
        await db.SaveChangesAsync();
}
