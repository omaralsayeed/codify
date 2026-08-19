using Codify.Domain.Enums;

namespace Codify.Application.DTOs.Contests;

public class ContestDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid CreatedByInstructorId { get; set; }
    public string InstructorName { get; set; } = string.Empty;

    public List<Guid> ProblemIds { get; set; } = [];
    public List<ContestProblemDetailDto> Problems { get; set; } = [];
    public List<Guid> AssignedStudentIds { get; set; } = [];

    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    
    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public ContestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ContestProblemDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public int Points { get; set; }
    public int Order { get; set; }
}
