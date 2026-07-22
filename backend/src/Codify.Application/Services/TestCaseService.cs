using Codify.Application.DTOs.TestCases;
using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Domain.Exceptions;

namespace Codify.Application.Services;

public class TestCaseService(
    ITestCaseRepository testCaseRepository,
    IProblemRepository problemRepository) : ITestCaseService
{
    public async Task<TestCaseResponse> CreateAsync(Guid problemId, CreateTestCaseDto request)
    {
        var problem = await problemRepository.GetByIdWithDetailsAsync(problemId)
            ?? throw new NotFoundException($"Problem {problemId} not found.");

        var testCase = TestCase.Create(
            problemId:      problem.Id,
            inputData:      request.Input,
            expectedOutput: request.ExpectedOutput,
            isSample:       request.IsSample,
            visibilityMode: request.VisibilityMode,
            orderIndex:     request.OrderIndex);

        await testCaseRepository.AddAsync(testCase);
        await testCaseRepository.SaveChangesAsync();

        return MapToResponse(testCase);
    }

    public async Task<List<TestCaseResponse>> GetByProblemIdAsync(Guid problemId, bool isInstructor)
    {
        var problem = await problemRepository.GetByIdWithDetailsAsync(problemId)
            ?? throw new NotFoundException($"Problem {problemId} not found.");

        var testCases = await testCaseRepository.GetByProblemIdAsync(problem.Id);

        if (!isInstructor)
            testCases = testCases.Where(tc => tc.IsSample).ToList();

        return testCases.Select(MapToResponse).ToList();
    }

    public async Task<TestCaseResponse> GetByIdAsync(Guid id, bool isInstructor)
    {
        var testCase = await testCaseRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"TestCase {id} not found.");

        if (!isInstructor && !testCase.IsSample)
            throw new ForbiddenException("You do not have access to this test case.");

        return MapToResponse(testCase);
    }

    public async Task<TestCaseResponse> UpdateAsync(Guid id, UpdateTestCaseDto request)
    {
        var testCase = await testCaseRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"TestCase {id} not found.");

        testCase.Update(
            inputData:      request.Input,
            expectedOutput: request.ExpectedOutput,
            isSample:       request.IsSample,
            visibilityMode: request.VisibilityMode,
            orderIndex:     request.OrderIndex);

        await testCaseRepository.SaveChangesAsync();

        return MapToResponse(testCase);
    }

    public async Task DeleteAsync(Guid id)
    {
        var testCase = await testCaseRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"TestCase {id} not found.");

        testCase.SoftDelete();
        await testCaseRepository.SaveChangesAsync();
    }

    private static TestCaseResponse MapToResponse(TestCase tc) => new()
    {
        Id             = tc.Id,
        ProblemId      = tc.ProblemId,
        Input          = tc.InputData,
        ExpectedOutput = tc.ExpectedOutput,
        IsSample       = tc.IsSample,
        VisibilityMode = tc.VisibilityMode,
        OrderIndex     = tc.OrderIndex,
        CreatedAt      = tc.CreatedAt
    };
}
