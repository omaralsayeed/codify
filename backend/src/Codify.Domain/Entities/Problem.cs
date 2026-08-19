using System.Text.Json;
using Codify.Domain.Enums;

namespace Codify.Domain.Entities;

public class Problem
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Statement { get; private set; } = string.Empty;
    public Difficulty Difficulty { get; private set; }
    public string LanguageSupportJson { get; private set; } = "[]";
    public string Constraints { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public bool IsActive { get; private set; }

    // ER diagram additions
    public Guid? AuthorId { get; private set; }
    public int TimeLimitMs { get; private set; }
    public int MemoryLimitMb { get; private set; }
    public bool IsPublic { get; private set; }
    public int AcceptedSubmissionsCount { get; private set; }
    public int TotalSubmissionsCount { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public string? CodeTemplateJson { get; private set; }

    // Navigation
    public User? Author { get; private set; }
    public ICollection<ProblemTag> ProblemTags { get; private set; } = [];
    public ICollection<TestCase> TestCases { get; private set; } = [];
    public ICollection<Submission> Submissions { get; private set; } = [];

    private Problem() { }

    public static Problem Create(
        string title,
        string statement,
        Difficulty difficulty,
        string constraints,
        string languageSupportJson,
        Guid? authorId = null,
        int timeLimitMs = 2000,
        int memoryLimitMb = 256)
    {
        var slug = GenerateSlug(title);
        return new Problem
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = slug,
            Statement = statement,
            Difficulty = difficulty,
            Constraints = constraints,
            LanguageSupportJson = languageSupportJson,
            AuthorId = authorId,
            TimeLimitMs = timeLimitMs,
            MemoryLimitMb = memoryLimitMb,
            IsActive = true,
            IsPublic = true,
            AcceptedSubmissionsCount = 0,
            TotalSubmissionsCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void Update(string title, string statement, Difficulty difficulty, string constraints, string languageSupportJson)
    {
        Title = title;
        Slug = GenerateSlug(title);
        Statement = statement;
        Difficulty = difficulty;
        Constraints = constraints;
        LanguageSupportJson = languageSupportJson;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Toggles the problem's active/inactive state.
    /// Inactive problems are hidden from students but kept in the database.
    /// </summary>
    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        IsPublic = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Updates execution resource limits.</summary>
    public void UpdateLimits(int timeLimitMs, int memoryLimitMb)
    {
        TimeLimitMs = timeLimitMs;
        MemoryLimitMb = memoryLimitMb;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementSubmissionCounters(bool accepted)
    {
        TotalSubmissionsCount++;
        if (accepted) AcceptedSubmissionsCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        IsPublic = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deserializes the CodeTemplateJson field into a dictionary of templates per language.
    /// Returns null if no templates are defined.
    /// </summary>
    public Dictionary<string, CodeTemplate>? GetCodeTemplates()
    {
        if (string.IsNullOrWhiteSpace(CodeTemplateJson))
            return null;
            
        return JsonSerializer.Deserialize<Dictionary<string, CodeTemplate>>(CodeTemplateJson);
    }

    private static string GenerateSlug(string title) =>
        title.ToLowerInvariant()
             .Replace(" ", "-")
             .Replace("_", "-")
             .Where(c => char.IsLetterOrDigit(c) || c == '-')
             .Aggregate(string.Empty, (acc, c) => acc + c)
             .Trim('-');
}
