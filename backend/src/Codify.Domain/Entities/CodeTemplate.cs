namespace Codify.Domain.Entities;

/// <summary>
/// Defines a code template for a specific programming language.
/// Templates specify function signatures and how to parse inputs/format outputs
/// so users can write only function logic (like LeetCode).
/// </summary>
public class CodeTemplate
{
    public string FunctionName { get; set; } = string.Empty;
    public List<ParameterDefinition> Parameters { get; set; } = new();
    public string ReturnType { get; set; } = string.Empty;
    public string OutputFormatter { get; set; } = string.Empty;
    public string StarterCode { get; set; } = string.Empty;
}

/// <summary>
/// Defines a single parameter for a function template.
/// </summary>
public class ParameterDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string InputParser { get; set; } = string.Empty;
}
