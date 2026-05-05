using System.ComponentModel.DataAnnotations;
using Codify.Domain.Enums;

namespace Codify.Application.DTOs.AI;

public class HintRequest
{
    public const int MinHintLevel = 1;
    public const int MaxHintLevel = 3;

    [Required]
    public Guid ProblemId { get; set; }

    [Required]
    public string StudentCode { get; set; } = string.Empty;

    [Range(MinHintLevel, MaxHintLevel)]
    public int HintLevel { get; set; }

    public List<string> PreviousHints { get; set; } = [];

    public int? AttemptCount { get; set; }

    public SubmissionStatus? LastSubmissionStatus { get; set; }
}
