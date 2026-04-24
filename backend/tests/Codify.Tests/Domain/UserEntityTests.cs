using Codify.Domain.Entities;
using Codify.Domain.Enums;

namespace Codify.Tests.Domain;

public class UserEntityTests
{
    [Fact]
    public void Create_ShouldSetAllRequiredFields()
    {
        var user = User.Create("Ahmed Hassan", "ahmed@example.com", "hash123", UserRole.Student);

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("Ahmed Hassan", user.FullName);
        Assert.Equal("ahmed@example.com", user.Email);
        Assert.Equal("hash123", user.PasswordHash);
        Assert.Equal(UserRole.Student, user.Role);
        Assert.True(user.CreatedAt <= DateTime.UtcNow);
        Assert.Null(user.LastLoginAt);
    }

    [Fact]
    public void Create_ShouldInitializeErDiagramFields()
    {
        var user = User.Create("Test User", "test@example.com", "hash", UserRole.Instructor);

        Assert.Equal(0, user.Rating);
        Assert.Equal(0, user.SolvedProblems);
        Assert.False(user.IsDeleted);
        Assert.True(user.UpdatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void RecordLogin_ShouldSetLastLoginAt()
    {
        var user = User.Create("Test", "test@example.com", "hash", UserRole.Student);
        var before = DateTime.UtcNow;

        user.RecordLogin();

        Assert.NotNull(user.LastLoginAt);
        Assert.True(user.LastLoginAt >= before);
    }

    [Fact]
    public void SoftDelete_ShouldMarkAsDeleted()
    {
        var user = User.Create("Test", "test@example.com", "hash", UserRole.Student);

        user.SoftDelete();

        Assert.True(user.IsDeleted);
    }

    [Fact]
    public void IncrementSolvedProblems_ShouldIncrementCounter()
    {
        var user = User.Create("Test", "test@example.com", "hash", UserRole.Student);

        user.IncrementSolvedProblems();
        user.IncrementSolvedProblems();

        Assert.Equal(2, user.SolvedProblems);
    }
}
