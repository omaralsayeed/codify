using Codify.Application.DTOs.Auth;
using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Domain.Enums;
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
        var user = User.Create(request.FullName, request.Email, passwordHash, request.Role, request.Organization);

        await userRepo.AddAsync(user);
        await userRepo.SaveChangesAsync();

        return new RegisterResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role,
            Status = user.Status
        };
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await userRepo.GetByEmailAsync(request.Email)
            ?? throw new ValidationException("Invalid email or password.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new ValidationException("Invalid email or password.");

        // Pending instructors cannot log in until an admin approves their account
        if (user.Status == UserStatus.Pending)
            throw new PendingApprovalException("Your account is pending admin approval. Please check your email.");

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
                Role = user.Role,
                AvatarUrl = user.AvatarUrl
            }
        };
    }

    public async Task<UserProfileResponse> GetCurrentUserAsync(Guid userId)
    {
        var user = await userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("User not found.");

        return MapToUserProfile(user);
    }

    public async Task<UserProfileResponse> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
    {
        var user = await userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("User not found.");

        user.UpdateProfile(dto.FullName, dto.Bio, dto.Organization, dto.AvatarUrl);
        await userRepo.SaveChangesAsync();

        return MapToUserProfile(user);
    }

    public async Task UpdateAvatarUrlAsync(Guid userId, string avatarUrl)
    {
        var user = await userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("User not found.");

        user.UpdateProfile(user.FullName, user.Bio, user.Organization, avatarUrl);
        await userRepo.SaveChangesAsync();
    }

    private static UserProfileResponse MapToUserProfile(User user) => new()
    {
        UserId = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role,
        Status = user.Status,
        Organization = user.Organization,
        Bio = user.Bio,
        AvatarUrl = user.AvatarUrl,
        SolvedProblems = user.SolvedProblems,
        Rating = user.Rating,
        CreatedAt = user.CreatedAt
    };
}
