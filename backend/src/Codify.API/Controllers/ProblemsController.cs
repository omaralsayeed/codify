using Codify.API.Common;
using Codify.API.Extensions;
using Codify.Application.DTOs.Problems;
using Codify.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codify.API.Controllers;

[ApiController]
[Route("api/problems")]
[Authorize]
public class ProblemsController(IProblemService problemService, ISubmissionService submissionService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ProblemFilterRequest filter)
    {
        var isInstructor = User.IsInRole("Instructor");
        var result = await problemService.GetAllAsync(filter, isInstructor);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await problemService.GetByIdAsync(id);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> Create([FromBody] CreateProblemRequest request)
    {
        var authorId = User.GetUserId();
        var result = await problemService.CreateAsync(request, authorId);
        return StatusCode(201, ApiResponse.Ok(result));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProblemRequest request)
    {
        var result = await problemService.UpdateAsync(id, request);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await problemService.DeleteAsync(id);
        return Ok(ApiResponse.Ok(null));
    }

    [HttpGet("{id:guid}/submissions")]
    public async Task<IActionResult> GetSubmissions(Guid id)
    {
        var userId = User.GetUserId();
        var isInstructor = User.IsInRole("Instructor");
        var result = await submissionService.GetByProblemAsync(id, userId, isInstructor);
        return Ok(ApiResponse.Ok(result));
    }
}
