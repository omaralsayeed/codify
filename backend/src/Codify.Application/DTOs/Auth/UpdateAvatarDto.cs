using System.ComponentModel.DataAnnotations;

namespace Codify.Application.DTOs.Auth;

public class UpdateAvatarDto
{
    [Required]
    [MaxLength(500)]
    [Url]
    public string AvatarUrl { get; set; } = string.Empty;
}
