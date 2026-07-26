using System.Text.Json;
using Codify.Application.DTOs.Analytics;
using Codify.Application.Interfaces;
using Codify.Domain.Enums;
using Codify.Domain.Exceptions;

namespace Codify.Application.Services;

public class AnalyticsService(IUserRepository userRepository) : IAnalyticsService
{
    // ────────────────────────────────────────────────────────────────
    // Student analytics (unchanged)
    // ────────────────────────────────────────────────────────────────

    public async Task<StudentAnalyticsResponse> GetStudentAnalyticsAsync(Guid studentId)
    {
        var user = await userRepository.GetWithAnalyticsDataAsync(studentId)
            ?? throw new NotFoundException($"Student {studentId} not found.");

        var submissions = user.Submissions.ToList();

        var totalSubmissions  = submissions.Count;
        var accepted          = submissions.Count(s => s.Status == SubmissionStatus.Accepted);
        var wrongAnswers      = submissions.Count(s => s.Status == SubmissionStatus.WrongAnswer);
        var runtimeErrors     = submissions.Count(s => s.Status == SubmissionStatus.RuntimeError);
        var compileErrors     = submissions.Count(s => s.Status == SubmissionStatus.CompileError);
        var timeLimitExceeded = submissions.Count(s => s.Status == SubmissionStatus.TimeLimitExceeded);

        var successRate = totalSubmissions > 0
            ? Math.Round((double)accepted / totalSubmissions * 100, 2)
            : 0;

        var acceptedSubmissions = submissions.Where(s => s.Status == SubmissionStatus.Accepted).ToList();

        var solvedProblemIds = acceptedSubmissions.Select(s => s.ProblemId).Distinct().ToHashSet();

        var easySolved = acceptedSubmissions
            .Where(s => solvedProblemIds.Contains(s.ProblemId) && s.Problem?.Difficulty == Difficulty.Easy)
            .Select(s => s.ProblemId).Distinct().Count();

        var mediumSolved = acceptedSubmissions
            .Where(s => solvedProblemIds.Contains(s.ProblemId) && s.Problem?.Difficulty == Difficulty.Medium)
            .Select(s => s.ProblemId).Distinct().Count();

        var hardSolved = acceptedSubmissions
            .Where(s => solvedProblemIds.Contains(s.ProblemId) && s.Problem?.Difficulty == Difficulty.Hard)
            .Select(s => s.ProblemId).Distinct().Count();

        var acceptedWithTime = acceptedSubmissions.Where(s => s.ExecutionTimeMs.HasValue).ToList();
        double? avgExecutionTimeMs = acceptedWithTime.Count > 0
            ? Math.Round(acceptedWithTime.Average(s => s.ExecutionTimeMs!.Value), 2)
            : null;

        double avgAttempts = submissions.Count > 0 && solvedProblemIds.Count > 0
            ? Math.Round((double)totalSubmissions / Math.Max(solvedProblemIds.Count, 1), 2)
            : 0;

        var languageBreakdown = submissions
            .GroupBy(s => s.Language.ToString())
            .Select(g => new LanguageStatItem { Language = g.Key, Submissions = g.Count() })
            .OrderByDescending(l => l.Submissions)
            .ToList();

        var strongTopics = ParseJsonArray(user.PerformanceProfile?.StrongTopicsJson);
        var weakTopics   = ParseJsonArray(user.PerformanceProfile?.WeakTopicsJson);

        return new StudentAnalyticsResponse
        {
            UserId                    = user.Id,
            FullName                  = user.FullName,
            Email                     = user.Email,
            TotalSolvedProblems       = solvedProblemIds.Count,
            EasySolved                = easySolved,
            MediumSolved              = mediumSolved,
            HardSolved                = hardSolved,
            TotalSubmissions          = totalSubmissions,
            AcceptedSubmissions       = accepted,
            WrongAnswers              = wrongAnswers,
            RuntimeErrors             = runtimeErrors,
            CompileErrors             = compileErrors,
            TimeLimitExceeded         = timeLimitExceeded,
            SuccessRatePercent        = successRate,
            AverageExecutionTimeMs    = avgExecutionTimeMs,
            AverageAttemptsPerProblem = avgAttempts,
            LanguageBreakdown         = languageBreakdown,
            StrongTopics              = strongTopics,
            WeakTopics                = weakTopics,
            LastSubmissionAt          = submissions.Count > 0 ? submissions.Max(s => s.SubmittedAt) : null,
            MemberSince               = user.CreatedAt
        };
    }

    // ────────────────────────────────────────────────────────────────
    // Instructor analytics
    // ────────────────────────────────────────────────────────────────

    public async Task<InstructorAnalyticsResponse> GetInstructorAnalyticsAsync(Guid instructorId)
    {
        var instructor = await userRepository.GetInstructorWithProblemsAndSubmissionsAsync(instructorId)
            ?? throw new NotFoundException($"Instructor {instructorId} not found.");

        if (instructor.Role != UserRole.Instructor)
            throw new ForbiddenException("The requested user is not an instructor.");

        var authoredProblems = instructor.AuthoredProblems.ToList();

        // All submissions across every authored problem
        var allSubmissions = authoredProblems
            .SelectMany(p => p.Submissions)
            .ToList();

        var totalSubmissions = allSubmissions.Count;
        var totalAccepted    = allSubmissions.Count(s => s.Status == SubmissionStatus.Accepted);

        var overallAcceptRate = totalSubmissions > 0
            ? Math.Round((double)totalAccepted / totalSubmissions * 100, 2)
            : 0;

        // Group submissions by student to build per-student summaries
        var submissionsByStudent = allSubmissions
            .GroupBy(s => s.UserId)
            .ToList();

        var studentSummaries = submissionsByStudent.Select(group =>
        {
            var studentSubmissions = group.ToList();

            // Grab student info from the first submission's User navigation
            var studentUser = studentSubmissions.First().User;

            var accepted     = studentSubmissions.Count(s => s.Status == SubmissionStatus.Accepted);
            var total        = studentSubmissions.Count;
            var successRate  = total > 0 ? Math.Round((double)accepted / total * 100, 2) : 0;

            var problemsSolved = studentSubmissions
                .Where(s => s.Status == SubmissionStatus.Accepted)
                .Select(s => s.ProblemId)
                .Distinct()
                .Count();

            return new StudentSummaryItem
            {
                StudentId           = group.Key,
                FullName            = studentUser?.FullName ?? "Unknown",
                Email               = studentUser?.Email    ?? string.Empty,
                TotalSubmissions    = total,
                AcceptedSubmissions = accepted,
                SuccessRatePercent  = successRate,
                ProblemsSolved      = problemsSolved,
                LastActivityAt      = studentSubmissions.Max(s => s.SubmittedAt)
            };
        })
        .OrderByDescending(s => s.ProblemsSolved)
        .ThenByDescending(s => s.SuccessRatePercent)
        .ToList();

        return new InstructorAnalyticsResponse
        {
            InstructorId             = instructor.Id,
            FullName                 = instructor.FullName,
            Email                    = instructor.Email,
            TotalProblemsAuthored    = authoredProblems.Count,
            TotalStudentsReached     = submissionsByStudent.Count,
            TotalSubmissionsReceived = totalSubmissions,
            OverallAcceptRatePercent = overallAcceptRate,
            Students                 = studentSummaries
        };
    }

    // ────────────────────────────────────────────────────────────────
    // Helper
    // ────────────────────────────────────────────────────────────────

    private static List<string> ParseJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; }
        catch (JsonException) { return []; }
    }
}
