using Codify.Application.DTOs.Auth;
using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Domain.Exceptions;

namespace Codify.Application.Services;

public class AuthService(IUserRepository userRepo, IJwtService jwtService) : IAuthService
{
    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        var existing = await userRepo.GetByEmailAsync(request.Email);
        if (existing is not null)
            throw new ValidationException("Email is already registered.");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var user = User.Create(request.FullName, request.Email, passwordHash, request.Role);

        await userRepo.AddAsync(user);
        await userRepo.SaveChangesAsync();

        return new RegisterResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role
        };
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await userRepo.GetByEmailAsync(request.Email)
            ?? throw new ValidationException("Invalid email or password.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new ValidationException("Invalid email or password.");

        user.RecordLogin();
        await userRepo.SaveChangesAsync();

        return new LoginResponse
        {
            Token = jwtService.GenerateToken(user),
            ExpiresAt = jwtService.GetExpiry(),
            User = new LoginUserInfo
            {
                UserId = user.Id,
                FullName = user.FullName,
                Role = user.Role
            }
        };
    }

    public async Task<UserProfileResponse> GetCurrentUserAsync(Guid userId)
    {
        var user = await userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("User not found.");

        return new UserProfileResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };
    }
}
