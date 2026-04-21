using Codify.Application.DTOs.Auth;

namespace Codify.Application.Interfaces;

public interface IAuthService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<UserProfileResponse> GetCurrentUserAsync(Guid userId);
}
