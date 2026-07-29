using Codify.API.Common;
using Codify.API.Extensions;
using Codify.Application.DTOs.Submissions;
using Codify.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Codify.API.Controllers;

[ApiController]
[Route("api/submissions")]
[Authorize]
public class SubmissionsController(ISubmissionService submissionService) : ControllerBase
{
    /// <summary>
    /// Submit code for a problem. Returns 202 Accepted with the pending submission.
    /// Rate limited: 30 requests per hour per user.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Student")]
    [EnableRateLimiting("submissions")]
    public async Task<IActionResult> Create([FromBody] CreateSubmissionRequest request)
    {
        var userId = User.GetUserId();
        var result = await submissionService.CreateAsync(request, userId);
        return StatusCode(202, ApiResponse.Ok(result));
    }

    /// <summary>
    /// Get the result of a specific submission.
    /// Students can only see their own; instructors can see all.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = User.GetUserId();
        var isInstructor = User.IsInRole("Instructor");
        var result = await submissionService.GetByIdAsync(id, userId, isInstructor);
        return Ok(ApiResponse.Ok(result));
    }
}
