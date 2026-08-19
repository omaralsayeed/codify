using Codify.Domain.Entities;

namespace Codify.Application.Interfaces;

/// <summary>
/// Service that wraps user-written function code with input/output handling
/// to create complete executable programs for Judge0.
/// </summary>
public interface ICodeWrapperService
{
    /// <summary>
    /// Wraps user code with input/output handling based on language template.
    /// </summary>
    /// <param name="userCode">The user's function implementation</param>
    /// <param name="language">Programming language</param>
    /// <param name="template">Code template for the problem</param>
    /// <returns>Complete executable code with I/O handling</returns>
    string WrapUserCode(string userCode, string language, CodeTemplate template);
    
    /// <summary>
    /// Checks if the problem uses code templates for the specified language.
    /// </summary>
    bool UsesTemplate(Problem problem, string language);
    
    /// <summary>
    /// Gets starter code for the language if template exists.
    /// </summary>
    string? GetStarterCode(Problem problem, string language);
}
