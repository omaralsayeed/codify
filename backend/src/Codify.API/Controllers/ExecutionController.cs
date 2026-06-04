using Codify.API.Common;
using Codify.Application.DTOs.Execution;
using Codify.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codify.API.Controllers;

[ApiController]
[Route("api/execution")]
//[Authorize]
public class ExecutionController(
    IExecutionService executionService,
    IQuickRunService quickRunService) : ControllerBase
{
    /// <summary>
    /// Run code against sample test cases only (used for the "Run" button before submitting).
    /// Does not persist a submission record.
    /// </summary>
    [HttpPost("run")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Run([FromBody] RunCodeRequest request)
    {
        var result = await executionService.RunAsync(request);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>
    /// Day 2 skeleton endpoint. Accepts a language and code snippet and acknowledges receipt.
    /// No execution logic is performed — this entry point will be wired to the real
    /// execution engine in a future sprint.
    /// </summary>
    [HttpPost("quick-run")]
    // [Authorize(Roles = "Student")]
    public async Task<IActionResult> QuickRun([FromBody] QuickRunRequest request)
    {
        var result = await quickRunService.RunAsync(request);
        return Ok(ApiResponse.Ok(result));
    }
}
