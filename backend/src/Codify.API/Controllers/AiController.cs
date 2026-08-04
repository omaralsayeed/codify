using Codify.API.Common;
using Codify.API.Extensions;
using Codify.Application.DTOs.AI;
using Codify.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Codify.API.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AiController(
    IAiHintService hintService,
    ICodeAnalysisService codeAnalysisService,
    IAnalyticsService analyticsService) : ControllerBase
{
    /// <summary>
    /// Request an AI-generated hint for a problem.
    /// Hint level is auto-incremented server-side. Max 3 hints per problem per user.
    /// Rate limited: 10 requests per hour per user.
    /// </summary>
    [HttpPost("hints")]
    [Authorize(Roles = "Student")]
    [EnableRateLimiting("ai-hints")]
    public async Task<IActionResult> GetHint(
        [FromBody] HintRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var result = await hintService.GetHintAsync(request, userId, cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPost("analyze")]
    [Authorize(Roles = "Student")]
    [EnableRateLimiting("ai-analyze")]
    public async Task<IActionResult> AnalyzeCode(
        [FromBody] CodeAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var result = await codeAnalysisService.AnalyzeAsync(request, userId, cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPost("analytics")]
    [Authorize(Roles = "Student")]
    [EnableRateLimiting("ai-analytics")]
    public async Task<IActionResult> RefreshAnalytics(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var result = await analyticsService.AnalyzeAsync(userId, cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("analytics")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetAnalytics(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var result = await analyticsService.GetAnalyticsAsync(userId, cancellationToken);
        return result is null
            ? NotFound(ApiResponse.Fail("NotFound", "Analytics not found. Generate them first."))
            : Ok(ApiResponse.Ok(result));
    }

    [HttpGet("analytics/{userId:guid}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> GetAnalyticsForStudent(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await analyticsService.GetAnalyticsAsync(userId, cancellationToken);
        return result is null
            ? NotFound(ApiResponse.Fail("NotFound", "Analytics not found for this student."))
            : Ok(ApiResponse.Ok(result));
    }
    /// <summary>
    /// Get the full hint history for the current user on a specific problem.
    /// Returns all hint logs ordered by hint level ascending.
    /// </summary>
    [HttpGet("hints/history")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetHintHistory(
        [FromQuery] Guid problemId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var result = await hintService.GetHintHistoryAsync(problemId, userId, cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }
}
