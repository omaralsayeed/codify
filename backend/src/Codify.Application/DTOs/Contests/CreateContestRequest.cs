using System.ComponentModel.DataAnnotations;

namespace Codify.Application.DTOs.Contests;

public class CreateContestRequest
{
    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Required]
    [MinLength(1, ErrorMessage = "A contest must contain at least one problem.")]
    public List<Guid> ProblemIds { get; set; } = [];

    public List<Guid> AssignedStudentIds { get; set; } = [];
    public List<string> StudentEmails { get; set; } = [];

    [Required]
    public DateTime StartAt { get; set; }

    [Required]
    public DateTime EndAt { get; set; }
}
