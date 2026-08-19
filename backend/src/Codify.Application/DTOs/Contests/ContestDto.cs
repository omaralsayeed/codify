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
    public List<ContestParticipantSummaryDto> Participants { get; set; } = [];

    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    
    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public ContestStatus Status { get; set; }

    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public InvitationStatus? MyInvitationStatus { get; set; }

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

public class ContestParticipantSummaryDto
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public InvitationStatus InvitationStatus { get; set; }
    public DateTime? RespondedAt { get; set; }
    public int Score { get; set; }
    public int ProblemsSolved { get; set; }
    public double Accuracy { get; set; }
    public int Rank { get; set; }
}

public class StudentCandidateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

