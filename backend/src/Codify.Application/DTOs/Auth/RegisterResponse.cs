using Codify.Domain.Enums;

namespace Codify.Application.DTOs.Auth;

public class RegisterResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
}
