using Codify.Domain.Enums;

namespace Codify.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    // Navigation
    public PerformanceProfile? PerformanceProfile { get; private set; }
    public ICollection<Submission> Submissions { get; private set; } = [];
    public ICollection<HintLog> HintLogs { get; private set; } = [];

    private User() { }

    public static User Create(string fullName, string email, string passwordHash, UserRole role)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void RecordLogin() => LastLoginAt = DateTime.UtcNow;
}
