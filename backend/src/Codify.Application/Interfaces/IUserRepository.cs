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

    Task AddAsync(User user);
    Task SaveChangesAsync();
}
