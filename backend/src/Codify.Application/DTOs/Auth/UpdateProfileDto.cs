namespace Codify.Application.DTOs.Auth;

public class UpdateProfileDto
{
    public string? FullName { get; set; }
    public string? Bio { get; set; }
    public string? Organization { get; set; }
    public string? AvatarUrl { get; set; }
}
