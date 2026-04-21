using Codify.API.Common;
using Codify.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codify.API.Controllers;

[ApiController]
[Route("api/tags")]
[Authorize]
public class TagsController(IConceptTagService tagService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await tagService.GetAllAsync();
        return Ok(ApiResponse.Ok(result));
    }
}
