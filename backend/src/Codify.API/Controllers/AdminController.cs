using Codify.API.Common;
using Codify.API.Extensions;
using Codify.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codify.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController(
    IAdminService adminService,
    IKnowledgeBaseIngestionService ingestionService) : ControllerBase
{
    /// <summary>
    /// Returns all instructors whose accounts are pending approval.
    /// </summary>
    [HttpGet("instructors/pending")]
    public async Task<IActionResult> GetPendingInstructors()
    {
        var result = await adminService.GetPendingInstructorsAsync();
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>
    /// Approves a pending instructor, allowing them to log in.
    /// </summary>
    [HttpPatch("instructors/{id:guid}/approve")]
    public async Task<IActionResult> ApproveInstructor(Guid id)
    {
        var adminId = User.GetUserId();
        var result = await adminService.ApproveInstructorAsync(id, adminId);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>
    /// GET /api/admin/stats
    /// Returns live platform aggregate statistics.
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await adminService.GetStatsAsync();
        return Ok(ApiResponse.Ok(stats));
    }

    /// <summary>
    /// GET /api/admin/users
    /// Returns all registered users in the database across all roles.
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await adminService.GetAllUsersAsync();
        return Ok(ApiResponse.Ok(users));
    }

    /// <summary>
    /// GET /api/admin/users/{id}
    /// Returns full user detail and submission history.
    /// </summary>
    [HttpGet("users/{id:guid}")]
    public async Task<IActionResult> GetUserDetail(Guid id)
    {
        var user = await adminService.GetUserDetailAsync(id);
        return Ok(ApiResponse.Ok(user));
    }

    /// <summary>
    /// PATCH /api/admin/users/{id}/status
    /// Updates a user's status.
    /// </summary>
    [HttpPatch("users/{id:guid}/status")]
    public async Task<IActionResult> UpdateUserStatus(Guid id, [FromBody] Codify.Application.DTOs.Admin.UpdateUserStatusRequest request)
    {
        var success = await adminService.UpdateUserStatusAsync(id, request.Status);
        return Ok(ApiResponse.Ok(new { success }));
    }

    /// <summary>
    /// Reindexes all concept tags and problems into the Chroma Cloud knowledge base.
    /// This populates the RAG layer so the Tutor Agent can retrieve grounded context.
    /// </summary>
    [HttpPost("rag/reindex")]
    public async Task<IActionResult> ReindexKnowledgeBase(CancellationToken ct)
    {
        var result = await ingestionService.ReindexAllAsync(ct);
        return Ok(ApiResponse.Ok(new
        {
            conceptsIngested = result.ConceptsIngested,
            problemsIngested = result.ProblemsIngested,
            totalIngested = result.TotalIngested
        }));
    }
}
