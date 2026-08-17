using Codify.Domain.Enums;

namespace Codify.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public UserStatus Status { get; private set; }
    public string? Organization { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    // Approval audit
    public Guid? ReviewedBy { get; private set; }
    public DateTime? ReviewedAt { get; private set; }

    // ER diagram additions
    public string? Username { get; private set; }
    public string? Bio { get; private set; }
    public string? AvatarUrl { get; private set; }
    public decimal Rating { get; private set; }
    public int SolvedProblems { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation
    public PerformanceProfile? PerformanceProfile { get; private set; }
    public ICollection<Submission> Submissions { get; private set; } = [];
    public ICollection<HintLog> HintLogs { get; private set; } = [];
    public ICollection<Problem> AuthoredProblems { get; private set; } = [];

    private User() { }

    public static User Create(string fullName, string email, string passwordHash, UserRole role, string? organization = null)
    {
        // Instructors start as pending; students are immediately active
        var status = role == UserRole.Instructor ? UserStatus.Pending : UserStatus.Active;

        return new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            Status = status,
            Organization = organization,
            Rating = 0,
            SolvedProblems = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void RecordLogin() => LastLoginAt = DateTime.UtcNow;

    /// <summary>
    /// General-purpose status setter for admin use.
    /// Sets the user to Active or Pending and records the admin who made the change.
    /// Use this for the admin panel "activate / set-pending" toggle.
    /// </summary>
    public void SetStatus(UserStatus status, Guid adminId)
    {
        Status = status;
        ReviewedBy = adminId;
        ReviewedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Approves a pending instructor account. Convenience wrapper around SetStatus(Active).
    /// </summary>
    public void Approve(Guid approvedByAdminId) => SetStatus(UserStatus.Active, approvedByAdminId);

    public void UpdateProfile(string? fullName, string? bio, string? organization, string? avatarUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(fullName))
            FullName = fullName.Trim();
        Bio = bio;
        Organization = organization;
        if (avatarUrl is not null)
            AvatarUrl = avatarUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementSolvedProblems()
    {
        SolvedProblems++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
