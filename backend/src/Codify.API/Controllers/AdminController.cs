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
