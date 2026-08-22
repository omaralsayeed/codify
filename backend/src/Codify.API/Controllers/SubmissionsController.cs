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
    /// Submit code for a problem. Returns 202 Accepted immediately with the submission in
    /// "Pending"/"Running" status; evaluation happens asynchronously against Judge0.
    /// Poll GET /submissions/{id} for the final verdict, and the Code Checker Agent runs
    /// in the background afterward — poll GET /submissions/{id}/feedback for its notes.
    /// Rate limited: 30 requests per hour per user.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Student,Instructor")]
    [EnableRateLimiting("submissions")]
    public async Task<IActionResult> Create([FromBody] CreateSubmissionRequest request)
    {
        var userId = User.GetUserId();
        var result = await submissionService.CreateAsync(request, userId);
        return StatusCode(202, ApiResponse.Ok(result));
    }

    /// <summary>
    /// Get a specific submission by ID.
    /// Students can only see their own; instructors can see all.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId       = User.GetUserId();
        var isInstructor = User.IsInRole("Instructor");
        var result       = await submissionService.GetByIdAsync(id, userId, isInstructor);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>
    /// Get all AI feedback records for a submission.
    /// The feedback is generated asynchronously after submission — allow a few seconds.
    /// Students can only see their own feedback; instructors can see all.
    /// </summary>
    [HttpGet("{id:guid}/feedback")]
    public async Task<IActionResult> GetFeedback(Guid id)
    {
        var userId       = User.GetUserId();
        var isInstructor = User.IsInRole("Instructor");
        var result       = await submissionService.GetFeedbackAsync(id, userId, isInstructor);
        return Ok(ApiResponse.Ok(result));
    }
}
