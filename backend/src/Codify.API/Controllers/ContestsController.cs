using Codify.API.Common;
using Codify.API.Extensions;
using Codify.Application.DTOs.Contests;
using Codify.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codify.API.Controllers;

[ApiController]
[Route("api/contests")]
[Authorize]
public class ContestsController(IContestService contestService) : ControllerBase
{
    /// <summary>
    /// GET /api/contests
    /// Returns all contests in the platform.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetInstructorContests()
    {
        var instructorId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : Guid.Empty;
        var contests = await contestService.GetInstructorContestsAsync(instructorId);
        return Ok(ApiResponse.Ok(contests));
    }

    /// <summary>
    /// GET /api/contests/my-contests
    /// Returns active live contests, upcoming contests, and historical completed contests for the student.
    /// </summary>
    [HttpGet("my-contests")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMyStudentContests()
    {
        var studentId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : Guid.Empty;
        var overview = await contestService.GetStudentContestsOverviewAsync(studentId);
        return Ok(ApiResponse.Ok(overview));
    }

    /// <summary>
    /// GET /api/contests/{id}
    /// Returns contest details and included problems.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetContestById(Guid id)
    {
        var contest = await contestService.GetContestByIdAsync(id);
        return Ok(ApiResponse.Ok(contest));
    }

    /// <summary>
    /// POST /api/contests
    /// Creates a new contest assigned to problems and students.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Instructor,Admin")]
    public async Task<IActionResult> CreateContest([FromBody] CreateContestRequest request)
    {
        var instructorId = User.GetUserId();
        var created = await contestService.CreateContestAsync(instructorId, request);
        return CreatedAtAction(nameof(GetContestById), new { id = created.Id }, ApiResponse.Ok(created));
    }

    /// <summary>
    /// GET /api/contests/{id}/results
    /// Returns ranked leaderboard results for a contest.
    /// </summary>
    [HttpGet("{id:guid}/results")]
    public async Task<IActionResult> GetContestResults(Guid id)
    {
        var results = await contestService.GetContestResultsAsync(id);
        return Ok(ApiResponse.Ok(results));
    }

    /// <summary>
    /// GET /api/contests/students/{studentId}/history
    /// Returns historical contest results for a student.
    /// </summary>
    [HttpGet("students/{studentId:guid}/history")]
    public async Task<IActionResult> GetStudentContestHistory(Guid studentId)
    {
        var requesterId = User.GetUserId();
        var isInstructor = User.IsInRole("Instructor") || User.IsInRole("Admin");

        if (!isInstructor && requesterId != studentId)
            return Forbid();

        var history = await contestService.GetStudentContestHistoryAsync(studentId);
        return Ok(ApiResponse.Ok(history));
    }

    /// <summary>
    /// POST /api/contests/{id}/invitations/respond
    /// Allows a student to accept or decline a contest invitation.
    /// </summary>
    [HttpPost("{id:guid}/invitations/respond")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> RespondToInvitation(Guid id, [FromBody] RespondContestInvitationRequest request)
    {
        var studentId = User.GetUserId();
        await contestService.RespondToInvitationAsync(id, studentId, request.Accept);
        return Ok(ApiResponse.Ok(new
        {
            contestId = id,
            accepted = request.Accept,
            message = request.Accept ? "Contest invitation accepted successfully." : "Contest invitation declined."
        }));
    }
}
