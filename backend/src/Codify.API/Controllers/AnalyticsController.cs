using Codify.API.Common;
using Codify.API.Extensions;
using Codify.Application.Interfaces;
using Codify.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codify.API.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize]
public class AnalyticsController(IAnalyticsService analyticsService) : ControllerBase
{
    /// <summary>
    /// Returns analytics for a single student.
    /// Instructors can query any student. Students can only query themselves.
    /// </summary>
    [HttpGet("students/{studentId:guid}")]
    public async Task<IActionResult> GetStudentAnalytics(Guid studentId)
    {
        var callerId     = User.GetUserId();
        var isInstructor = User.IsInRole("Instructor");

        if (!isInstructor && callerId != studentId)
            throw new ForbiddenException("You can only view your own analytics.");

        var result = await analyticsService.GetStudentAnalyticsAsync(studentId);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>
    /// Convenience route: returns the analytics for the currently logged-in student.
    /// </summary>
    [HttpGet("me")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMyAnalytics()
    {
        var userId = User.GetUserId();
        var result = await analyticsService.GetStudentAnalyticsAsync(userId);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>
    /// Returns the instructor's dashboard: all students who submitted on their
    /// authored problems, with per-student summaries.
    /// Instructors can query themselves or any other instructor.
    /// </summary>
    [HttpGet("instructors/{instructorId:guid}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> GetInstructorAnalytics(Guid instructorId)
    {
        var result = await analyticsService.GetInstructorAnalyticsAsync(instructorId);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>
    /// Convenience route: returns the analytics for the currently logged-in instructor.
    /// </summary>
    [HttpGet("instructor/me")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> GetMyInstructorAnalytics()
    {
        var instructorId = User.GetUserId();
        var result       = await analyticsService.GetInstructorAnalyticsAsync(instructorId);
        return Ok(ApiResponse.Ok(result));
    }
}
