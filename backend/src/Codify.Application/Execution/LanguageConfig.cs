namespace Codify.Application.Execution;

/// <summary>
/// Holds everything Docker needs to know to run code in a specific language.
/// Adding a new language = adding one new entry here. Nothing else changes.
/// </summary>
public class LanguageConfig
{
    public string DockerImage { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string RunCommand { get; init; } = string.Empty;
    public string? ProjectFileContent { get; init; }
    public string? ProjectFileName { get; init; }

    /// <summary>
    /// How long to wait before killing the container.
    /// C# needs more time because dotnet run compiles before executing (~30-40s first run).
    /// </summary>
    public int TimeoutMs { get; init; } = 5000;

    private static readonly Dictionary<string, LanguageConfig> Configs = new(
        StringComparer.OrdinalIgnoreCase)
    {
        ["python"] = new LanguageConfig
        {
            DockerImage = "python:3.11",
            FileName    = "main.py",
            RunCommand  = "python /app/main.py",
            TimeoutMs   = 5000
        },

        ["csharp"] = new LanguageConfig
        {
            DockerImage        = "mcr.microsoft.com/dotnet/sdk:8.0",
            FileName           = "Program.cs",
            RunCommand         = "dotnet run --project /app/app.csproj",
            TimeoutMs          = 60000,
            ProjectFileName    = "app.csproj",
            ProjectFileContent =
                "<Project Sdk=\"Microsoft.NET.Sdk\">\n" +
                "  <PropertyGroup>\n" +
                "    <OutputType>Exe</OutputType>\n" +
                "    <TargetFramework>net8.0</TargetFramework>\n" +
                "    <Nullable>enable</Nullable>\n" +
                "    <ImplicitUsings>enable</ImplicitUsings>\n" +
                "  </PropertyGroup>\n" +
                "</Project>"
        }
    };

    public static LanguageConfig? Get(string language)
        => Configs.TryGetValue(language, out var config) ? config : null;

    public static string SupportedLanguages
        => string.Join(", ", Configs.Keys);
}
