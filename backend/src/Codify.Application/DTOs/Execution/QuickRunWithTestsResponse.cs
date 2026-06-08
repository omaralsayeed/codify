namespace Codify.Application.DTOs.Execution;

/// <summary>
/// Response for the test-cases execution endpoint.
/// </summary>
public class QuickRunWithTestsResponse
{
    /// <summary>Results for every test case in the same order they were sent.</summary>
    public List<TestCaseResult> Results { get; set; } = [];

    /// <summary>How many test cases passed.</summary>
    public int PassedCount => Results.Count(r => r.Passed);

    /// <summary>Total number of test cases.</summary>
    public int TotalCount => Results.Count;

    /// <summary>True only if every single test case passed.</summary>
    public bool AllPassed => PassedCount == TotalCount;
}
