using Codify.Domain.Enums;

namespace Codify.Application.DTOs.Auth;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public LoginUserInfo User { get; set; } = null!;
}

public class LoginUserInfo
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
}
