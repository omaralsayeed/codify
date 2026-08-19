using Codify.API.Common;
using Codify.API.Extensions;
using Codify.Application.DTOs.Admin;
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
    // ── Legacy instructor approval endpoints ──────────────────────────────────

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

    // ── Admin panel: overview ─────────────────────────────────────────────────

    /// <summary>
    /// Platform-wide statistics for the admin overview dashboard.
    /// GET /api/admin/stats
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var result = await adminService.GetStatsAsync();
        return Ok(ApiResponse.Ok(result));
    }

    // ── Admin panel: user management ──────────────────────────────────────────

    /// <summary>
    /// Paginated, filterable list of all students and instructors (admins excluded).
    /// GET /api/admin/users
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] AdminUserFilterRequest filter)
    {
        var (users, total) = await adminService.GetUsersAsync(filter);
        return Ok(ApiResponse.Ok(new
        {
            users,
            total,
            page     = filter.Page,
            pageSize = filter.PageSize
        }));
    }

    /// <summary>
    /// Full detail for a single user including stats and recent submissions.
    /// GET /api/admin/users/:id
    /// </summary>
    [HttpGet("users/{id:guid}")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var result = await adminService.GetUserByIdAsync(id);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>
    /// Activate or set-pending a user. Cannot be used on admin accounts.
    /// PATCH /api/admin/users/:id/status
    /// </summary>
    [HttpPatch("users/{id:guid}/status")]
    public async Task<IActionResult> UpdateUserStatus(
        Guid id, [FromBody] UpdateUserStatusRequest request)
    {
        var adminId = User.GetUserId();
        var result  = await adminService.UpdateUserStatusAsync(id, request.Status, adminId);
        return Ok(ApiResponse.Ok(result));
    }

    // ── Admin panel: problem management ──────────────────────────────────────

    /// <summary>
    /// Paginated, filterable list of ALL problems including inactive ones.
    /// Unlike GET /api/problems which only returns active problems.
    /// GET /api/admin/problems
    /// </summary>
    [HttpGet("problems")]
    public async Task<IActionResult> GetProblems([FromQuery] AdminProblemFilterRequest filter)
    {
        var (problems, total) = await adminService.GetAdminProblemsAsync(filter);
        return Ok(ApiResponse.Ok(new
        {
            problems,
            total,
            page     = filter.Page,
            pageSize = filter.PageSize
        }));
    }

    // ── RAG management ────────────────────────────────────────────────────────

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
            totalIngested    = result.TotalIngested
        }));
    }
}
