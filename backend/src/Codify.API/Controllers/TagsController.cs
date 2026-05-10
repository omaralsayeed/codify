using Codify.API.Common;
using Codify.Application.DTOs.Tags;
using Codify.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codify.API.Controllers;

// [ApiController] gives us automatic model validation (400 if [Required] fields are missing)
// and automatic binding of [FromBody], [FromQuery] etc.
[ApiController]
[Route("api/tags")]
[Authorize]
public class TagsController(IConceptTagService tagService) : ControllerBase
{
    // ── ConceptTag CRUD ──────────────────────────────────────────────────

    /// <summary>GET /api/tags — returns all non-deleted concept tags.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await tagService.GetAllAsync();
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>GET /api/tags/{id} — returns a single tag by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await tagService.GetByIdAsync(id);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>POST /api/tags — create a new concept tag. Instructor only.</summary>
    [HttpPost]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> Create([FromBody] CreateConceptTagRequest request)
    {
        var result = await tagService.CreateAsync(request);
        // 201 Created is the correct HTTP status for resource creation
        return StatusCode(201, ApiResponse.Ok(result));
    }

    /// <summary>PUT /api/tags/{id} — update name/description of a tag. Instructor only.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateConceptTagRequest request)
    {
        var result = await tagService.UpdateAsync(id, request);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>DELETE /api/tags/{id} — soft-deletes a tag. Instructor only.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await tagService.DeleteAsync(id);
        return Ok(ApiResponse.Ok(null));
    }

    // ── ProblemTag (join table) endpoints ────────────────────────────────
    // Route pattern: /api/tags/problems/{problemId}/tags mirrors "what tags does problem X have?"

    /// <summary>GET /api/tags/problems/{problemId} — list all tags applied to a problem.</summary>
    [HttpGet("problems/{problemId:guid}")]
    public async Task<IActionResult> GetTagsForProblem(Guid problemId)
    {
        var result = await tagService.GetTagsForProblemAsync(problemId);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>
    /// POST /api/tags/problems/{problemId}/{conceptTagId} — tag a problem with a concept.
    /// Instructor only.
    /// </summary>
    [HttpPost("problems/{problemId:guid}/{conceptTagId:guid}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> AddTagToProblem(Guid problemId, Guid conceptTagId)
    {
        await tagService.AddTagToProblemAsync(problemId, conceptTagId);
        return StatusCode(201, ApiResponse.Ok(null));
    }

    /// <summary>
    /// DELETE /api/tags/problems/{problemId}/{conceptTagId} — remove a tag from a problem.
    /// Instructor only.
    /// </summary>
    [HttpDelete("problems/{problemId:guid}/{conceptTagId:guid}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> RemoveTagFromProblem(Guid problemId, Guid conceptTagId)
    {
        await tagService.RemoveTagFromProblemAsync(problemId, conceptTagId);
        return Ok(ApiResponse.Ok(null));
    }
}
