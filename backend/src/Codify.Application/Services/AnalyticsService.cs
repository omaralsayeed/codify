using System.Text.Json;
using Codify.Application.DTOs.Analytics;
using Codify.Application.Interfaces;
using Codify.Domain.Enums;
using Codify.Domain.Exceptions;

namespace Codify.Application.Services;

public class AnalyticsService(IUserRepository userRepo) : IAnalyticsService
{
    // ─────────────────────────────────────────────────────────────────────────
    // Student analytics
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<StudentAnalyticsResponse> GetStudentAnalyticsAsync(Guid targetUserId)
    {
        var user = await userRepo.GetWithAnalyticsDataAsync(targetUserId)
            ?? throw new NotFoundException($"User {targetUserId} not found.");

        var submissions = user.Submissions.ToList();

        // ── Submission counts by status ───────────────────────────────────────
        int total      = submissions.Count;
        int accepted   = submissions.Count(s => s.Status == SubmissionStatus.Accepted);
        int wrong      = submissions.Count(s => s.Status == SubmissionStatus.WrongAnswer);
        int runtime    = submissions.Count(s => s.Status == SubmissionStatus.RuntimeError);
        int compile    = submissions.Count(s => s.Status == SubmissionStatus.CompileError);
        int tle        = submissions.Count(s => s.Status == SubmissionStatus.TimeLimitExceeded);

        double successRate = total > 0 ? Math.Round((double)accepted / total * 100, 1) : 0;

        // ── Average execution time across Accepted submissions ─────────────────
        var acceptedWithTime = submissions
            .Where(s => s.Status == SubmissionStatus.Accepted && s.ExecutionTimeMs.HasValue)
            .ToList();

        double? avgExecTime = acceptedWithTime.Count > 0
            ? Math.Round(acceptedWithTime.Average(s => (double)s.ExecutionTimeMs!.Value), 1)
            : null;

        // ── Distinct problems solved, split by difficulty ─────────────────────
        var solvedProblems = submissions
            .Where(s => s.Status == SubmissionStatus.Accepted)
            .GroupBy(s => s.ProblemId)
            .Select(g => g.First().Problem)
            .ToList();

        int easySolved   = solvedProblems.Count(p => p.Difficulty == Difficulty.Easy);
        int mediumSolved = solvedProblems.Count(p => p.Difficulty == Difficulty.Medium);
        int hardSolved   = solvedProblems.Count(p => p.Difficulty == Difficulty.Hard);

        // ── Average attempts per problem ──────────────────────────────────────
        int distinctProblems = submissions.Select(s => s.ProblemId).Distinct().Count();
        double avgAttempts   = distinctProblems > 0
            ? Math.Round((double)total / distinctProblems, 2)
            : 0;

        // ── Language breakdown ────────────────────────────────────────────────
        var languageBreakdown = submissions
            .GroupBy(s => s.Language.ToString())
            .Select(g => new LanguageStatItem { Language = g.Key, Submissions = g.Count() })
            .OrderByDescending(x => x.Submissions)
            .ToList();

        // ── Weak / strong topics from persisted PerformanceProfile ────────────
        var weakTopics   = new List<string>();
        var strongTopics = new List<string>();

        if (user.PerformanceProfile is not null)
        {
            weakTopics   = JsonSerializer.Deserialize<List<string>>(
                user.PerformanceProfile.WeakTopicsJson)   ?? [];
            strongTopics = JsonSerializer.Deserialize<List<string>>(
                user.PerformanceProfile.StrongTopicsJson) ?? [];
        }

        // ── Last activity ─────────────────────────────────────────────────────
        DateTime? lastSubmission = submissions.Count > 0
            ? submissions.Max(s => s.SubmittedAt)
            : null;

        return new StudentAnalyticsResponse
        {
            UserId                    = user.Id,
            FullName                  = user.FullName,
            Email                     = user.Email,
            TotalSolvedProblems       = solvedProblems.Count,
            EasySolved                = easySolved,
            MediumSolved              = mediumSolved,
            HardSolved                = hardSolved,
            TotalSubmissions          = total,
            AcceptedSubmissions       = accepted,
            WrongAnswers              = wrong,
            RuntimeErrors             = runtime,
            CompileErrors             = compile,
            TimeLimitExceeded         = tle,
            SuccessRatePercent        = successRate,
            AverageExecutionTimeMs    = avgExecTime,
            AverageAttemptsPerProblem = avgAttempts,
            LanguageBreakdown         = languageBreakdown,
            WeakTopics                = weakTopics,
            StrongTopics              = strongTopics,
            TotalHintsUsed            = user.PerformanceProfile?.TotalHintsUsed ?? 0,
            LastSubmissionAt          = lastSubmission,
            MemberSince               = user.CreatedAt
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Instructor analytics
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<InstructorAnalyticsResponse> GetInstructorOverviewAsync(Guid instructorId)
    {
        var instructor = await userRepo.GetInstructorWithProblemsAndSubmissionsAsync(instructorId)
            ?? throw new NotFoundException($"Instructor {instructorId} not found.");

        var problems = instructor.AuthoredProblems.ToList();

        // Flatten all submissions across this instructor's problems
        var allSubmissions = problems
            .SelectMany(p => p.Submissions)
            .ToList();

        int totalReceived = allSubmissions.Count;
        int totalAccepted = allSubmissions.Count(s => s.Status == SubmissionStatus.Accepted);
        double acceptRate = totalReceived > 0
            ? Math.Round((double)totalAccepted / totalReceived * 100, 1)
            : 0;

        // Distinct students who submitted on at least one of this instructor's problems
        var studentIds = allSubmissions
            .Select(s => s.UserId)
            .Distinct()
            .ToHashSet();

        // Build per-student summary (scoped to this instructor's problems only)
        var students = allSubmissions
            .GroupBy(s => s.UserId)
            .Select(g =>
            {
                // All submissions for this student in this instructor's problem set
                var studentSubs  = g.ToList();
                var subUser      = studentSubs.First().User;
                int stuTotal     = studentSubs.Count;
                int stuAccepted  = studentSubs.Count(s => s.Status == SubmissionStatus.Accepted);
                double stuRate   = stuTotal > 0
                    ? Math.Round((double)stuAccepted / stuTotal * 100, 1)
                    : 0;
                int problemsSolved = studentSubs
                    .Where(s => s.Status == SubmissionStatus.Accepted)
                    .Select(s => s.ProblemId)
                    .Distinct()
                    .Count();
                DateTime? lastActivity = studentSubs.Max(s => (DateTime?)s.SubmittedAt);

                return new StudentSummaryItem
                {
                    StudentId            = g.Key,
                    FullName             = subUser?.FullName ?? string.Empty,
                    Email                = subUser?.Email    ?? string.Empty,
                    TotalSubmissions     = stuTotal,
                    AcceptedSubmissions  = stuAccepted,
                    SuccessRatePercent   = stuRate,
                    ProblemsSolved       = problemsSolved,
                    LastActivityAt       = lastActivity
                };
            })
            .OrderByDescending(s => s.LastActivityAt)
            .ToList();

        return new InstructorAnalyticsResponse
        {
            InstructorId               = instructor.Id,
            FullName                   = instructor.FullName,
            Email                      = instructor.Email,
            TotalProblemsAuthored      = problems.Count,
            TotalStudentsReached       = studentIds.Count,
            TotalSubmissionsReceived   = totalReceived,
            OverallAcceptRatePercent   = acceptRate,
            Students                   = students
        };
    }
}
