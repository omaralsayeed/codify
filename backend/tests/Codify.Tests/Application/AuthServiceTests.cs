using Codify.Application.DTOs.Auth;
using Codify.Application.Interfaces;
using Codify.Application.Services;
using Codify.Domain.Entities;
using Codify.Domain.Enums;
using Codify.Domain.Exceptions;
using NSubstitute;

namespace Codify.Tests.Application;

public class AuthServiceTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_userRepo, _jwtService);
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateUser_WhenEmailIsNew()
    {
        _userRepo.GetByEmailAsync("new@example.com").Returns((User?)null);

        var request = new RegisterRequest
        {
            FullName = "New User",
            Email = "new@example.com",
            Password = "password123",
            Role = UserRole.Student
        };

        var result = await _sut.RegisterAsync(request);

        Assert.Equal("new@example.com", result.Email);
        Assert.Equal(UserRole.Student, result.Role);
        await _userRepo.Received(1).AddAsync(Arg.Any<User>());
        await _userRepo.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowValidationException_WhenEmailAlreadyExists()
    {
        var existing = User.Create("Existing", "taken@example.com", "hash", UserRole.Student);
        _userRepo.GetByEmailAsync("taken@example.com").Returns(existing);

        var request = new RegisterRequest
        {
            FullName = "New User",
            Email = "taken@example.com",
            Password = "password123",
            Role = UserRole.Student
        };

        await Assert.ThrowsAsync<ValidationException>(() => _sut.RegisterAsync(request));
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnToken_WhenCredentialsAreValid()
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("correctpassword");
        var user = User.Create("Test User", "user@example.com", passwordHash, UserRole.Student);

        _userRepo.GetByEmailAsync("user@example.com").Returns(user);
        _jwtService.GenerateToken(user).Returns("fake-jwt-token");
        _jwtService.GetExpiry().Returns(DateTime.UtcNow.AddHours(1));

        var request = new LoginRequest { Email = "user@example.com", Password = "correctpassword" };

        var result = await _sut.LoginAsync(request);

        Assert.Equal("fake-jwt-token", result.Token);
        Assert.Equal(user.Id, result.User.UserId);
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowValidationException_WhenPasswordIsWrong()
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("correctpassword");
        var user = User.Create("Test User", "user@example.com", passwordHash, UserRole.Student);

        _userRepo.GetByEmailAsync("user@example.com").Returns(user);

        var request = new LoginRequest { Email = "user@example.com", Password = "wrongpassword" };

        await Assert.ThrowsAsync<ValidationException>(() => _sut.LoginAsync(request));
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowValidationException_WhenUserNotFound()
    {
        _userRepo.GetByEmailAsync("nobody@example.com").Returns((User?)null);

        var request = new LoginRequest { Email = "nobody@example.com", Password = "password" };

        await Assert.ThrowsAsync<ValidationException>(() => _sut.LoginAsync(request));
    }

    [Fact]
    public async Task GetCurrentUserAsync_ShouldReturnProfile_WhenUserExists()
    {
        var user = User.Create("Ahmed", "ahmed@example.com", "hash", UserRole.Instructor);
        _userRepo.GetByIdAsync(user.Id).Returns(user);

        var result = await _sut.GetCurrentUserAsync(user.Id);

        Assert.Equal(user.Id, result.UserId);
        Assert.Equal("Ahmed", result.FullName);
        Assert.Equal(UserRole.Instructor, result.Role);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ShouldThrowNotFoundException_WhenUserDoesNotExist()
    {
        var id = Guid.NewGuid();
        _userRepo.GetByIdAsync(id).Returns((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetCurrentUserAsync(id));
    }
}
