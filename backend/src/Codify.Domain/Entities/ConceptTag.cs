namespace Codify.Domain.Entities;

public class ConceptTag
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation
    public ICollection<ProblemTag> ProblemTags { get; private set; } = [];

    private ConceptTag() { }

    public static ConceptTag Create(string name, string description)
    {
        return new ConceptTag
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = GenerateSlug(name),
            Description = description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void Update(string name, string description)
    {
        Name = name;
        Slug = GenerateSlug(name);
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    private static string GenerateSlug(string name) =>
        name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("&", "and")
            .Where(c => char.IsLetterOrDigit(c) || c == '-')
            .Aggregate(string.Empty, (acc, c) => acc + c)
            .Trim('-');
}
