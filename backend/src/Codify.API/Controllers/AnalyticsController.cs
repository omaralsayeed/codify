using Codify.API.Common;
using Codify.API.Extensions;
using Codify.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Codify.API.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize]
[EnableRateLimiting("analytics")]
public class AnalyticsController(IAnalyticsService analyticsService) : ControllerBase
{
    /// <summary>
    /// GET /api/analytics/students/{id}
    /// Returns the full performance breakdown for a student.
    /// Students can only view their own data.
    /// Instructors can view any student's data.
    /// </summary>
    [HttpGet("students/{id:guid}")]
    public async Task<IActionResult> GetStudentAnalytics(Guid id)
    {
        var requesterId  = User.GetUserId();
        var isInstructor = User.IsInRole("Instructor");

        // Students may only query their own profile
        if (!isInstructor && requesterId != id)
            return Forbid();

        var result = await analyticsService.GetStudentAnalyticsAsync(id);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>
    /// GET /api/analytics/overview
    /// Returns cohort-level analytics for the authenticated instructor:
    /// problems authored, students reached, accept rate, per-student summaries.
    /// </summary>
    [HttpGet("overview")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> GetInstructorOverview()
    {
        var instructorId = User.GetUserId();
        var result       = await analyticsService.GetInstructorOverviewAsync(instructorId);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>
    /// GET /api/analytics/integrity-flags
    /// Returns all AI-generated code flags for instructor review.
    /// Instructor-only.
    /// </summary>
    [HttpGet("integrity-flags")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> GetIntegrityFlags()
    {
        var result = await analyticsService.GetIntegrityFlagsAsync();
        return Ok(ApiResponse.Ok(result));
    }
}
