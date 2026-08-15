// AI endpoints. Hints live in HintsController (/api/hints). This controller hosts
// the AI tagging endpoints driven by the Tagging Agent (a static workflow).

using Codify.API.Common;
using Codify.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Codify.API.Controllers;

[ApiController]
[Route("api/ai")]
public class AiController(ITaggingService taggingService) : ControllerBase
{
    /// <summary>
    /// POST /api/ai/tagging/{problemId}
    /// Runs the Tagging Agent to classify and apply concept tags to one problem.
    /// Instructor-only. If the problem already has tags it is left unchanged.
    /// </summary>
    [HttpPost("tagging/{problemId:guid}")]
    [Authorize(Roles = "Instructor")]
    [EnableRateLimiting("ai-tagging")]
    public async Task<IActionResult> TagProblem(Guid problemId, CancellationToken cancellationToken)
    {
        var result = await taggingService.TagProblemAsync(problemId, cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }
}

