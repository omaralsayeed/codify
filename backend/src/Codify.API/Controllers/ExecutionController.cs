using Codify.API.Common;
using Codify.Application.DTOs.Execution;
using Codify.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codify.API.Controllers;

[ApiController]
[Route("api/execution")]
[Authorize]
public class ExecutionController(IExecutionService executionService) : ControllerBase
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
}
