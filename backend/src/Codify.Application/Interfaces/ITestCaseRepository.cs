using Codify.Domain.Entities;

namespace Codify.Application.Interfaces;

public interface ITestCaseRepository
{
    Task<List<TestCase>> GetByProblemIdAsync(Guid problemId);
    Task<TestCase?> GetByIdAsync(Guid id);
    Task AddAsync(TestCase testCase);
    Task SaveChangesAsync();
}
