using Codify.API.Common;
using Codify.API.Extensions;
using Codify.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codify.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController(IAdminService adminService) : ControllerBase
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
}
