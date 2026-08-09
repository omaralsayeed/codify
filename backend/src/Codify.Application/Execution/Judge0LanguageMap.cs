namespace Codify.Application.Execution;

/// <summary>
/// Maps Codify's language identifiers (same strings used by <see cref="LanguageConfig"/>)
/// to Judge0 CE's numeric language_id.
/// Adding a new language = adding one new entry here. Nothing else changes.
/// </summary>
public static class Judge0LanguageMap
{
    private static readonly Dictionary<string, int> LanguageIds = new(
        StringComparer.OrdinalIgnoreCase)
    {
        ["python"] = 71, // Python 3.8.1
        ["csharp"] = 51  // C# (Mono 6.6.0.161)
    };

    public static int? GetLanguageId(string language)
        => LanguageIds.TryGetValue(language, out var id) ? id : null;

    public static string SupportedLanguages
        => string.Join(", ", LanguageIds.Keys);
}
