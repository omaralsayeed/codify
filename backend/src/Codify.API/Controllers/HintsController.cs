using Codify.API.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Codify.API.Controllers;

/// <summary>
/// AI hint endpoints. Full implementation wired to the Tutor Agent in Sprint 3 (Omar).
/// Rate limiting policy is already applied: 10 requests per hour per user.
/// </summary>
[ApiController]
[Route("api/ai/hints")]
[Authorize(Roles = "Student")]
public class HintsController : ControllerBase
{
    /// <summary>
    /// Request a hint for a problem.
    /// Rate limited: 10 requests per hour per user.
    /// TODO Sprint 3: inject IHintService and wire to Tutor Agent.
    /// </summary>
    [HttpPost]
    [EnableRateLimiting("ai-hints")]
    public IActionResult RequestHint()
    {
        // Placeholder — full implementation in Sprint 3
        return StatusCode(501, ApiResponse.Fail("NOT_IMPLEMENTED",
            "Hint service will be available in Sprint 3."));
    }

    /// <summary>
    /// Get hint history for the current user on a problem.
    /// TODO Sprint 3: inject IHintService.
    /// </summary>
    [HttpGet("history")]
    public IActionResult GetHistory([FromQuery] Guid problemId)
    {
        // Placeholder — full implementation in Sprint 3
        return StatusCode(501, ApiResponse.Fail("NOT_IMPLEMENTED",
            "Hint service will be available in Sprint 3."));
    }
}
