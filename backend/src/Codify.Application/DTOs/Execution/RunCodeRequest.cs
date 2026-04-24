using System.ComponentModel.DataAnnotations;
using Codify.Domain.Enums;

namespace Codify.Application.DTOs.Execution;

public class RunCodeRequest
{
    [Required]
    public Guid ProblemId { get; set; }

    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    public SubmissionLanguage Language { get; set; }
}
