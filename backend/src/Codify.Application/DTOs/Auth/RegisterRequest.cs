using System.ComponentModel.DataAnnotations;
using Codify.Domain.Enums;

namespace Codify.Application.DTOs.Auth;

public class RegisterRequest
{
    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }
}
