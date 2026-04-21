namespace Codify.Domain.Entities;

public class ConceptTag
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    // Navigation
    public ICollection<ProblemTag> ProblemTags { get; private set; } = [];

    private ConceptTag() { }

    public static ConceptTag Create(string name, string description)
    {
        return new ConceptTag
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description
        };
    }
}
