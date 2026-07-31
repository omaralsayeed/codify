using Codify.API.Common;
using Codify.API.Extensions;
using Codify.Application.DTOs.AI;
using Codify.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Codify.API.Controllers;

/// <summary>
/// AI hint endpoints. Wired to the Tutor Agent via IAiHintService.
/// Rate limiting policy is applied: 10 requests per hour per user.
/// </summary>
[ApiController]
[Route("api/hints")]
[Authorize(Roles = "Student")]
public class HintsController(IAiHintService hintService) : ControllerBase
{
    /// <summary>
    /// Request an AI-generated hint for a problem.
    /// Rate limited: 10 requests per hour per user.
    /// </summary>
    [HttpPost]
    [EnableRateLimiting("ai-hints")]
    public async Task<IActionResult> RequestHint(
        [FromBody] HintRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var result = await hintService.GetHintAsync(request, userId, cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>
    /// Get hint history for the current user on a problem.
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] Guid problemId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var result = await hintService.GetHintHistoryAsync(problemId, userId, cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }
}

