using Codify.Domain.Entities;

namespace Codify.Tests.Domain;

public class ConceptTagEntityTests
{
    [Fact]
    public void Create_ShouldSetAllFields()
    {
        var tag = ConceptTag.Create("Dynamic Programming", "Breaking problems into overlapping subproblems.");

        Assert.NotEqual(Guid.Empty, tag.Id);
        Assert.Equal("Dynamic Programming", tag.Name);
        Assert.Equal("dynamic-programming", tag.Slug);
        Assert.False(tag.IsDeleted);
        Assert.True(tag.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Create_ShouldGenerateSlugWithAmpersand()
    {
        var tag = ConceptTag.Create("Arrays & Hashing", "...");

        Assert.Equal("arrays-and-hashing", tag.Slug);
    }

    [Fact]
    public void Create_ShouldGenerateSlugForMultiWord()
    {
        var tag = ConceptTag.Create("Binary Search", "...");

        Assert.Equal("binary-search", tag.Slug);
    }
}
