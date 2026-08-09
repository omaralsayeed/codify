using Codify.Domain.Entities;

namespace Codify.Application.Interfaces;

public interface ITestCaseResultRepository
{
    Task AddRangeAsync(IEnumerable<TestCaseResult> results);
    Task<List<TestCaseResult>> GetBySubmissionIdAsync(Guid submissionId);
    Task SaveChangesAsync();
}
