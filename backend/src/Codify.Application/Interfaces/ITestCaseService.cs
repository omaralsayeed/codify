using Codify.Application.DTOs.TestCases;

namespace Codify.Application.Interfaces;

public interface ITestCaseService
{
    Task<TestCaseResponse> CreateAsync(Guid problemId, CreateTestCaseDto request);
    Task<List<TestCaseResponse>> GetByProblemIdAsync(Guid problemId, bool isInstructor);
    Task<TestCaseResponse> GetByIdAsync(Guid id, bool isInstructor);
    Task<TestCaseResponse> UpdateAsync(Guid id, UpdateTestCaseDto request);
    Task DeleteAsync(Guid id);
}
