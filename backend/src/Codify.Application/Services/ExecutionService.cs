using Codify.Application.DTOs.Execution;
using Codify.Application.Interfaces;
using Codify.Domain.Exceptions;

namespace Codify.Application.Services;

/// <summary>
/// Stub execution service. The real Docker-based runner will be implemented in Sprint 2 (Badry).
/// This stub simulates running code against sample test cases for the "Run" button flow.
/// </summary>
public class ExecutionService(IProblemRepository problemRepo) : IExecutionService
{
    public async Task<RunCodeResponse> RunAsync(RunCodeRequest request)
    {
        var problem = await problemRepo.GetByIdWithTestCasesAsync(request.ProblemId)
            ?? throw new NotFoundException($"Problem {request.ProblemId} not found.");

        var sampleCases = problem.TestCases
            .Where(tc => tc.IsSample)
            .OrderBy(tc => tc.OrderIndex)
            .ToList();

        if (sampleCases.Count == 0)
        {
            return new RunCodeResponse
            {
                Status = "NoSampleCases",
                Stdout = string.Empty,
                Stderr = "No sample test cases available for this problem.",
                ExecutionTimeMs = 0,
                TestResults = []
            };
        }

        // TODO: Replace with real Docker execution engine (Sprint 2 - Badry)
        // For now, return a stub response indicating the submission is queued
        return new RunCodeResponse
        {
            Status = "Pending",
            Stdout = string.Empty,
            Stderr = string.Empty,
            ExecutionTimeMs = 0,
            TestResults = sampleCases.Select(tc => new SampleTestResult
            {
                Input = tc.InputData,
                ExpectedOutput = tc.ExpectedOutput,
                ActualOutput = string.Empty,
                Passed = false
            }).ToList()
        };
    }
}
