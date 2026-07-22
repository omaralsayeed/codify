using Codify.API.Common;
using Codify.Application.DTOs.TestCases;
using Codify.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codify.API.Controllers;

[ApiController]
[Authorize]
public class TestCasesController(ITestCaseService testCaseService) : ControllerBase
{
    [HttpPost("api/problems/{problemId:guid}/testcases")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> Create(Guid problemId, [FromBody] CreateTestCaseDto request)
    {
        var result = await testCaseService.CreateAsync(problemId, request);
        return StatusCode(201, ApiResponse.Ok(result));
    }

    [HttpGet("api/problems/{problemId:guid}/testcases")]
    public async Task<IActionResult> GetByProblem(Guid problemId)
    {
        var isInstructor = User.IsInRole("Instructor");
        var result = await testCaseService.GetByProblemIdAsync(problemId, isInstructor);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("api/testcases/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var isInstructor = User.IsInRole("Instructor");
        var result = await testCaseService.GetByIdAsync(id, isInstructor);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPut("api/testcases/{id:guid}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTestCaseDto request)
    {
        var result = await testCaseService.UpdateAsync(id, request);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpDelete("api/testcases/{id:guid}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await testCaseService.DeleteAsync(id);
        return Ok(ApiResponse.Ok(null));
    }
}
