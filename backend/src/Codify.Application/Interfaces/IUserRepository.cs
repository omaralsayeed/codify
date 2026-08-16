using Codify.Domain.Entities;

namespace Codify.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Loads the user + their submissions (with Problem) + PerformanceProfile.
    /// Used by the student analytics query.
    /// </summary>
    Task<User?> GetWithAnalyticsDataAsync(Guid userId);

    /// <summary>
    /// Loads the instructor + their authored problems.
    /// Each problem includes all submissions (with the submitting User).
    /// Used by the instructor analytics query.
    /// </summary>
    Task<User?> GetInstructorWithProblemsAndSubmissionsAsync(Guid instructorId);

    /// <summary>Returns all active students with their submissions and performance profiles.</summary>
    Task<IReadOnlyList<User>> GetAllStudentsWithSubmissionsAsync();

    /// <summary>Finds a user by ID, username, email, or fullname slug with all submissions and problems.</summary>
    Task<User?> GetUserWithProfileDataAsync(string identifier);

    /// <summary>Returns all users across all roles.</summary>
    Task<IReadOnlyList<User>> GetAllUsersAsync();

    /// <summary>Returns all instructors with Status = Pending, ordered by registration date.</summary>
    Task<IReadOnlyList<User>> GetPendingInstructorsAsync();

    Task AddAsync(User user);
    Task SaveChangesAsync();
}
