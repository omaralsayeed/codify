using Codify.Application.DTOs.Analytics;

namespace Codify.Application.Interfaces;

public interface IAnalyticsService
{
    /// <summary>
    /// Returns analytics for one student.
    /// Instructors can query any student. Students can only query themselves.
    /// </summary>
    Task<StudentAnalyticsResponse> GetStudentAnalyticsAsync(Guid studentId);

    /// <summary>
    /// Returns the instructor's overview: all students who submitted on
    /// at least one of their authored problems, with per-student summaries.
    /// </summary>
    Task<InstructorAnalyticsResponse> GetInstructorAnalyticsAsync(Guid instructorId);
}
