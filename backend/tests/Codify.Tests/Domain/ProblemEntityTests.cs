using Codify.Domain.Entities;
using Codify.Domain.Enums;

namespace Codify.Tests.Domain;

public class ProblemEntityTests
{
    [Fact]
    public void Create_ShouldSetAllRequiredFields()
    {
        var problem = Problem.Create("Two Sum", "Given an array...", Difficulty.Easy, "1 <= n <= 10^4", "[]");

        Assert.NotEqual(Guid.Empty, problem.Id);
        Assert.Equal("Two Sum", problem.Title);
        Assert.Equal("two-sum", problem.Slug);
        Assert.Equal(Difficulty.Easy, problem.Difficulty);
        Assert.True(problem.IsActive);
        Assert.True(problem.IsPublic);
        Assert.False(problem.IsDeleted);
        Assert.Equal(0, problem.AcceptedSubmissionsCount);
        Assert.Equal(0, problem.TotalSubmissionsCount);
        Assert.Equal(2000, problem.TimeLimitMs);
        Assert.Equal(256, problem.MemoryLimitMb);
    }

    [Fact]
    public void Create_ShouldGenerateSlugFromTitle()
    {
        var problem = Problem.Create("Dynamic Programming Basics", "...", Difficulty.Medium, "", "[]");

        Assert.Equal("dynamic-programming-basics", problem.Slug);
    }

    [Fact]
    public void Create_ShouldAcceptAuthorId()
    {
        var authorId = Guid.NewGuid();
        var problem = Problem.Create("Test", "...", Difficulty.Easy, "", "[]", authorId);

        Assert.Equal(authorId, problem.AuthorId);
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveAndIsPublicToFalse()
    {
        var problem = Problem.Create("Test", "...", Difficulty.Easy, "", "[]");

        problem.Deactivate();

        Assert.False(problem.IsActive);
        Assert.False(problem.IsPublic);
    }

    [Fact]
    public void IncrementSubmissionCounters_ShouldUpdateBothCounters()
    {
        var problem = Problem.Create("Test", "...", Difficulty.Easy, "", "[]");

        problem.IncrementSubmissionCounters(accepted: true);
        problem.IncrementSubmissionCounters(accepted: false);
        problem.IncrementSubmissionCounters(accepted: true);

        Assert.Equal(3, problem.TotalSubmissionsCount);
        Assert.Equal(2, problem.AcceptedSubmissionsCount);
    }

    [Fact]
    public void Update_ShouldRegenerateSlug()
    {
        var problem = Problem.Create("Old Title", "...", Difficulty.Easy, "", "[]");

        problem.Update("New Title Here", "...", Difficulty.Hard, "", "[]");

        Assert.Equal("new-title-here", problem.Slug);
        Assert.Equal("New Title Here", problem.Title);
        Assert.Equal(Difficulty.Hard, problem.Difficulty);
    }
}
