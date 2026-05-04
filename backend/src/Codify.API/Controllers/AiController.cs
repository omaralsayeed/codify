using Codify.API.Common;
using Codify.API.Extensions;
using Codify.Application.DTOs.AI;
using Codify.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codify.API.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AiController(IAiHintService hintService) : ControllerBase
{
    [HttpPost("hints")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetHint(
        [FromBody] HintRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var result = await hintService.GetHintAsync(request, userId, cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }
}
