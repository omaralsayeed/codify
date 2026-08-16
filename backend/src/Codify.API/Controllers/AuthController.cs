using Codify.API.Common;
using Codify.API.Extensions;
using Codify.Application.DTOs.Auth;
using Codify.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codify.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request);
        return StatusCode(201, ApiResponse.Ok(result));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        // JWT is stateless — client discards the token
        return Ok(ApiResponse.Ok(null));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = User.GetUserId();
        var result = await authService.GetCurrentUserAsync(userId);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>
    /// Updates profile details (Full name, Bio, Organization) for the authenticated user.
    /// </summary>
    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var userId = User.GetUserId();
        var result = await authService.UpdateProfileAsync(userId, dto);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>
    /// Updates the avatar URL for the currently authenticated user.
    /// The URL must point to a Cloudinary resource.
    /// </summary>
    [HttpPut("avatar")]
    [Authorize]
    public async Task<IActionResult> UpdateAvatar([FromBody] UpdateAvatarDto dto)
    {
        if (!dto.AvatarUrl.StartsWith("https://res.cloudinary.com/", StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse.Fail("INVALID_AVATAR_URL", "avatarUrl must be a valid Cloudinary URL."));

        var userId = User.GetUserId();
        await authService.UpdateAvatarUrlAsync(userId, dto.AvatarUrl);
        return Ok(ApiResponse.Ok(null));
    }
}
