using Codify.Domain.Enums;

namespace Codify.Domain.Entities;

public class Problem
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Statement { get; private set; } = string.Empty;
    public Difficulty Difficulty { get; private set; }
    public string LanguageSupportJson { get; private set; } = "[]";
    public string Constraints { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public bool IsActive { get; private set; }

    // Navigation
    public ICollection<ProblemTag> ProblemTags { get; private set; } = [];
    public ICollection<TestCase> TestCases { get; private set; } = [];
    public ICollection<Submission> Submissions { get; private set; } = [];

    private Problem() { }

    public static Problem Create(
        string title,
        string statement,
        Difficulty difficulty,
        string constraints,
        string languageSupportJson)
    {
        return new Problem
        {
            Id = Guid.NewGuid(),
            Title = title,
            Statement = statement,
            Difficulty = difficulty,
            Constraints = constraints,
            LanguageSupportJson = languageSupportJson,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string title, string statement, Difficulty difficulty, string constraints, string languageSupportJson)
    {
        Title = title;
        Statement = statement;
        Difficulty = difficulty;
        Constraints = constraints;
        LanguageSupportJson = languageSupportJson;
    }

    public void Deactivate() => IsActive = false;
}
