using Codify.Application.DTOs.Analytics;

namespace Codify.Application.Interfaces;

public interface IAnalyticsService
{
    /// <summary>
    /// Returns the full performance breakdown for a student.
    /// Students can only query their own data; instructors can query any student.
    /// </summary>
    Task<StudentAnalyticsResponse> GetStudentAnalyticsAsync(Guid targetUserId, Guid? requestingInstructorId = null);

    /// <summary>
    /// Returns a cohort-level overview for an instructor:
    /// total problems authored, students reached, accept rate, per-student summaries.
    /// </summary>
    Task<InstructorAnalyticsResponse> GetInstructorOverviewAsync(Guid instructorId);

    /// <summary>
    /// Returns all AI-generated code flags for instructor review.
    /// </summary>
    Task<List<IntegrityFlagResponse>> GetIntegrityFlagsAsync(Guid? instructorId = null);

    /// <summary>
    /// Returns the public profile for any user by username, email, ID, or slug.
    /// </summary>
    Task<PublicProfileResponse> GetPublicProfileAsync(string identifier);
}
