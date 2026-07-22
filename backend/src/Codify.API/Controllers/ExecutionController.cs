using Codify.API.Common;
using Codify.Application.DTOs.Execution;
using Codify.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Codify.API.Controllers;

[ApiController]
[Route("api/execution")]
//[Authorize]
public class ExecutionController(
    IExecutionService executionService,
    IQuickRunService quickRunService,
    IQuickRunWithTestsService quickRunWithTestsService) : ControllerBase
{
    /// <summary>
    /// Run code against sample test cases only (used for the "Run" button before submitting).
    /// Does not persist a submission record.
    /// Rate limited: 60 requests per hour per user.
    /// </summary>
    [HttpPost("run")]
    [Authorize(Roles = "Student")]
    [EnableRateLimiting("execution")]
    public async Task<IActionResult> Run([FromBody] RunCodeRequest request)
    {
        var result = await executionService.RunAsync(request);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>
    /// Day 3 — run code and return raw stdout/stderr.
    /// </summary>
    [HttpPost("quick-run")]
    // [Authorize(Roles = "Student")]
    public async Task<IActionResult> QuickRun([FromBody] QuickRunRequest request)
    {
        var result = await quickRunService.RunAsync(request);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>
    /// Day 4 — run code against multiple test cases and return pass/fail for each.
    /// </summary>
    [HttpPost("run-with-tests")]
   // [Authorize(Roles = "Student")]
    public async Task<IActionResult> RunWithTests([FromBody] QuickRunWithTestsRequest request)
    {
        var result = await quickRunWithTestsService.RunAsync(request);
        return Ok(ApiResponse.Ok(result));
    }
}
