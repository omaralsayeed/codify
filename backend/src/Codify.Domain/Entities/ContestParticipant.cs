using Codify.Domain.Enums;

namespace Codify.Domain.Entities;

public class ContestParticipant
{
    public Guid ContestId { get; set; }
    public Contest Contest { get; set; } = null!;

    public Guid StudentId { get; set; }
    public User Student { get; set; } = null!;

    public InvitationStatus InvitationStatus { get; set; } = InvitationStatus.Pending;
    public string? InvitedEmail { get; set; }
    public DateTime? RespondedAt { get; set; }

    public int Score { get; set; }
    public int ProblemsSolved { get; set; }
    public double Accuracy { get; set; }
    public int Rank { get; set; }
    public DateTime? FinishedAt { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

