using System.Text.Json;
using Codify.Application.DTOs.Analytics;
using Codify.Application.Interfaces;
using Codify.Domain.Enums;
using Codify.Domain.Exceptions;

namespace Codify.Application.Services;

public class AnalyticsService(
    IUserRepository userRepo,
    IFeedbackRepository feedbackRepo,
    IProblemRepository problemRepo) : IAnalyticsService
{
    // ─────────────────────────────────────────────────────────────────────────
    // Student analytics
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<StudentAnalyticsResponse> GetStudentAnalyticsAsync(Guid targetUserId, Guid? requestingInstructorId = null)
    {
        if (requestingInstructorId.HasValue)
        {
            var isEnrolled = await userRepo.IsStudentEnrolledWithInstructorAsync(requestingInstructorId.Value, targetUserId);
            if (!isEnrolled)
                throw new ForbiddenException("You can only view analytics for your enrolled students.");
        }

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
        int medSolved    = solvedProblems.Count(p => p.Difficulty == Difficulty.Medium);
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

        // ── Strong / weak topics from PerformanceProfile ───────────────────────
        List<string> strongTopics = [];
        List<string> weakTopics   = [];

        if (user.PerformanceProfile != null)
        {
            strongTopics = DeserializeStringList(user.PerformanceProfile.StrongTopicsJson);
            weakTopics   = DeserializeStringList(user.PerformanceProfile.WeakTopicsJson);
        }

        return new StudentAnalyticsResponse
        {
            UserId                    = user.Id,
            FullName                  = user.FullName,
            Email                     = user.Email,
            TotalSolvedProblems       = solvedProblems.Count,
            EasySolved                = easySolved,
            MediumSolved              = medSolved,
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
            LastSubmissionAt          = submissions.Count > 0 ? submissions.Max(s => (DateTime?)s.SubmittedAt) : null,
            MemberSince               = user.CreatedAt
        };
    }

    private static List<string> DeserializeStringList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; }
        catch { return []; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Instructor analytics
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<InstructorAnalyticsResponse> GetInstructorOverviewAsync(Guid instructorId)
    {
        var instructor = await userRepo.GetInstructorWithProblemsAndSubmissionsAsync(instructorId)
            ?? throw new NotFoundException($"Instructor {instructorId} not found.");

        var authoredProblems = instructor.AuthoredProblems.ToList();
        var instructorStudents = await userRepo.GetStudentsForInstructorAsync(instructorId);
        var allActiveProblems = await problemRepo.GetAllActiveWithTagsAsync();
        var aiFlags = await feedbackRepo.GetAiGeneratedFlagsAsync();

        var instructorStudentIds = instructorStudents.Select(s => s.Id).ToHashSet();
        var cohortAiFlags = aiFlags.Where(f => f.Submission?.UserId != null && instructorStudentIds.Contains(f.Submission.UserId)).ToList();

        // Build per-student summary ONLY for students taught by this instructor
        var students = instructorStudents.Select(student =>
        {
            var studentSubs = student.Submissions.ToList();
            int stuTotal = studentSubs.Count;
            int stuAccepted = studentSubs.Count(s => s.Status == SubmissionStatus.Accepted);
            double stuRate = stuTotal > 0
                ? Math.Round((double)stuAccepted / stuTotal * 100, 1)
                : 0;
            int problemsSolved = student.SolvedProblems > 0
                ? student.SolvedProblems
                : studentSubs.Where(s => s.Status == SubmissionStatus.Accepted).Select(s => s.ProblemId).Distinct().Count();
            DateTime? lastActivity = studentSubs.Count > 0
                ? studentSubs.Max(s => (DateTime?)s.SubmittedAt)
                : student.LastLoginAt ?? student.CreatedAt;

            return new StudentSummaryItem
            {
                StudentId = student.Id,
                FullName = student.FullName,
                Email = student.Email,
                TotalSubmissions = stuTotal,
                AcceptedSubmissions = stuAccepted,
                SuccessRatePercent = stuRate,
                ProblemsSolved = problemsSolved,
                LastActivityAt = lastActivity
            };
        })
        .OrderByDescending(s => s.ProblemsSolved)
        .ThenByDescending(s => s.TotalSubmissions)
        .ThenBy(s => s.FullName)
        .ToList();

        // Submissions for this instructor's student cohort
        var allSubmissionsList = instructorStudents.SelectMany(s => s.Submissions).ToList();

        // Calculate overview stats
        var allAuthoredSubmissions = authoredProblems.SelectMany(p => p.Submissions).ToList();
        int totalReceived = allAuthoredSubmissions.Count > 0 ? allAuthoredSubmissions.Count : allSubmissionsList.Count;
        int totalAccepted = allAuthoredSubmissions.Count > 0
            ? allAuthoredSubmissions.Count(s => s.Status == SubmissionStatus.Accepted)
            : allSubmissionsList.Count(s => s.Status == SubmissionStatus.Accepted);
        double acceptRate = totalReceived > 0
            ? Math.Round((double)totalAccepted / totalReceived * 100, 1)
            : (students.Count > 0 && students.Any(s => s.TotalSubmissions > 0)
                ? Math.Round(students.Where(s => s.TotalSubmissions > 0).Average(s => s.SuccessRatePercent), 1)
                : 0);

        // Compute 14-day submission activity from real DB submissions
        var now = DateTime.UtcNow.Date;
        var dailyActivity = new List<DailySubmissionCountDto>();
        for (int i = 13; i >= 0; i--)
        {
            var day = now.AddDays(-i);
            int count = allSubmissionsList.Count(s => s.SubmittedAt.Date == day);
            dailyActivity.Add(new DailySubmissionCountDto
            {
                Date = day.ToString("yyyy-MM-dd"),
                DayLabel = day.ToString("MMM d"),
                Submissions = count
            });
        }

        // Compute real Topic Mastery from database problems and submissions
        var topicMasteryList = new List<TopicMasteryDto>();
        var distinctTags = allActiveProblems
            .SelectMany(p => p.ProblemTags)
            .Select(pt => pt.ConceptTag?.Name)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (distinctTags.Count == 0)
        {
            distinctTags = ["Arrays", "Strings", "Trees", "Dynamic Programming", "Graphs", "Binary Search", "Sorting", "Stack"];
        }

        foreach (var tag in distinctTags)
        {
            var problemIdsForTag = allActiveProblems
                .Where(p => p.ProblemTags.Any(pt => pt.ConceptTag != null && pt.ConceptTag.Name.Equals(tag, StringComparison.OrdinalIgnoreCase)))
                .Select(p => p.Id)
                .ToHashSet();

            var tagSubs = allSubmissionsList.Where(s => problemIdsForTag.Contains(s.ProblemId)).ToList();
            int tagTotal = tagSubs.Count;
            int tagAccepted = tagSubs.Count(s => s.Status == SubmissionStatus.Accepted);
            int pct = tagTotal > 0
                ? (int)Math.Round((double)tagAccepted / tagTotal * 100)
                : 0;

            topicMasteryList.Add(new TopicMasteryDto
            {
                Topic = tag!,
                Percentage = pct
            });
        }

        topicMasteryList = topicMasteryList.OrderByDescending(t => t.Percentage).ThenBy(t => t.Topic).ToList();

        return new InstructorAnalyticsResponse
        {
            InstructorId               = instructor.Id,
            FullName                   = instructor.FullName,
            Email                      = instructor.Email,
            TotalProblemsAuthored      = authoredProblems.Count,
            TotalStudentsReached       = instructorStudents.Count,
            TotalSubmissionsReceived   = totalReceived > 0 ? totalReceived : instructorStudents.Sum(s => s.Submissions.Count),
            OverallAcceptRatePercent   = acceptRate,
            TotalAssignedProblems      = allActiveProblems.Count,
            IntegrityFlagsCount        = cohortAiFlags.Count,
            DailyActivity              = dailyActivity,
            TopicPerformance           = topicMasteryList,
            Students                   = students
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Integrity flags (AI-generated code detection)
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<List<IntegrityFlagResponse>> GetIntegrityFlagsAsync(Guid? instructorId = null)
    {
        var flags = await feedbackRepo.GetAiGeneratedFlagsAsync();

        if (instructorId.HasValue)
        {
            var instructorStudents = await userRepo.GetStudentsForInstructorAsync(instructorId.Value);
            var allowedStudentIds = instructorStudents.Select(s => s.Id).ToHashSet();
            flags = flags.Where(f => f.Submission?.UserId != null && allowedStudentIds.Contains(f.Submission.UserId)).ToList();
        }

        return flags.Select(f => new IntegrityFlagResponse
        {
            FeedbackId    = f.Id,
            SubmissionId  = f.SubmissionId,
            StudentName   = f.Submission.User?.FullName ?? string.Empty,
            StudentEmail  = f.Submission.User?.Email ?? string.Empty,
            ProblemTitle  = f.Submission.Problem?.Title ?? string.Empty,
            ProblemId     = f.Submission.ProblemId,
            Confidence    = f.Confidence ?? 0,
            Indicators    = f.Message,
            FlaggedAt     = f.CreatedAt
        }).ToList();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public profile query
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<PublicProfileResponse> GetPublicProfileAsync(string identifier)
    {
        var user = await userRepo.GetUserWithProfileDataAsync(identifier);

        if (user == null)
        {
            // If not found by slug, fallback to first student or throw
            var students = await userRepo.GetAllStudentsWithSubmissionsAsync();
            user = students.FirstOrDefault()
                ?? throw new NotFoundException($"User '{identifier}' not found.");
        }

        var submissions = user.Submissions.ToList();
        var accepted = submissions.Where(s => s.Status == SubmissionStatus.Accepted).ToList();

        // 1. Solved problems distinct
        var solvedProblems = accepted
            .GroupBy(s => s.ProblemId)
            .Select(g => g.First().Problem)
            .Where(p => p != null)
            .ToList();

        int easySolved = solvedProblems.Count(p => p.Difficulty == Difficulty.Easy);
        int medSolved  = solvedProblems.Count(p => p.Difficulty == Difficulty.Medium);
        int hardSolved = solvedProblems.Count(p => p.Difficulty == Difficulty.Hard);

        // 2. Catalog totals
        var allProblems = await problemRepo.GetAllActiveWithTagsAsync();
        int totalEasy = allProblems.Count(p => p.Difficulty == Difficulty.Easy);
        int totalMed  = allProblems.Count(p => p.Difficulty == Difficulty.Medium);
        int totalHard = allProblems.Count(p => p.Difficulty == Difficulty.Hard);

        // 3. User initials
        var nameParts = user.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string initials = nameParts.Length > 1
            ? $"{nameParts[0][0]}{nameParts[1][0]}".ToUpper()
            : (user.FullName.Length > 0 ? user.FullName[..1].ToUpper() : "ST");

        // 4. Activity Grid (365 days)
        var today = DateTime.UtcNow.Date;
        var subDateMap = submissions
            .GroupBy(s => s.SubmittedAt.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var activityGrid = new List<ProfileActivityDayDto>(365);
        for (int i = 364; i >= 0; i--)
        {
            var day = today.AddDays(-i);
            int count = subDateMap.TryGetValue(day, out var c) ? c : 0;
            activityGrid.Add(new ProfileActivityDayDto
            {
                Date = day.ToString("yyyy-MM-dd"),
                Count = count
            });
        }

        // 5. Streaks
        int totalActiveDays = activityGrid.Count(d => d.Count > 0);
        int totalLastYear = activityGrid.Sum(d => d.Count);
        int currentStreak = 0;
        for (int i = activityGrid.Count - 1; i >= 0; i--)
        {
            if (activityGrid[i].Count > 0) currentStreak++;
            else if (i < activityGrid.Count - 1) break; // Allow today to not break yesterday streak yet
        }

        int longestStreak = 0;
        int run = 0;
        foreach (var d in activityGrid)
        {
            if (d.Count > 0) { run++; longestStreak = Math.Max(longestStreak, run); }
            else run = 0;
        }

        // 6. Language breakdown
        var languageStats = accepted
            .GroupBy(s => s.Language.ToString())
            .Select(g => new ProfileLanguageDto
            {
                Language = g.Key,
                Solved = g.Select(s => s.ProblemId).Distinct().Count()
            })
            .OrderByDescending(x => x.Solved)
            .ToList();

        // 7. Recent Accepted submissions
        var recentAccepted = accepted
            .OrderByDescending(s => s.SubmittedAt)
            .Take(8)
            .Select(s => new ProfileRecentSubmissionDto
            {
                SubmissionId = s.Id.ToString(),
                ProblemId = s.ProblemId.ToString(),
                ProblemTitle = s.Problem?.Title ?? "Problem",
                Difficulty = s.Problem?.Difficulty.ToString() ?? "Medium",
                Status = "Accepted",
                Language = s.Language.ToString(),
                SubmittedAt = s.SubmittedAt
            })
            .ToList();

        // 8. Topic performance from problem tags
        var topicStats = new List<ProfileTopicPerformanceDto>();
        var problemTags = allProblems
            .SelectMany(p => p.ProblemTags.Select(pt => new { Problem = p, Tag = pt.ConceptTag?.Name }))
            .Where(x => !string.IsNullOrEmpty(x.Tag))
            .GroupBy(x => x.Tag!)
            .ToList();

        int topicIdCounter = 1;
        foreach (var group in problemTags.Take(8))
        {
            var problemIds = group.Select(x => x.Problem.Id).ToHashSet();
            int attempted = submissions.Count(s => problemIds.Contains(s.ProblemId));
            int solved = accepted.Where(s => problemIds.Contains(s.ProblemId)).Select(s => s.ProblemId).Distinct().Count();
            int score = attempted > 0 ? (int)Math.Round((double)solved / attempted * 100) : 0;
            string strength = score >= 75 ? "strong" : (score >= 40 ? "average" : "weak");

            topicStats.Add(new ProfileTopicPerformanceDto
            {
                TopicId = $"t{topicIdCounter++}",
                TopicName = group.Key,
                Attempted = attempted,
                Solved = solved,
                StrengthScore = score,
                Strength = strength,
                AiInsight = strength == "weak" ? $"Practice more {group.Key} problems to improve accuracy." : null
            });
        }

        double successRate = submissions.Count > 0
            ? Math.Round((double)accepted.Count / submissions.Count * 100, 1)
            : 0;

        return new PublicProfileResponse
        {
            User = new ProfileUserDto
            {
                Username = !string.IsNullOrWhiteSpace(user.Username) ? user.Username : user.FullName.ToLower().Replace(" ", "_"),
                Name = user.FullName,
                AvatarInitials = initials,
                AvatarUrl = user.AvatarUrl,
                Role = user.Role.ToString().ToLower(),
                JoinedAt = user.CreatedAt,
                Headline = !string.IsNullOrWhiteSpace(user.Organization) ? user.Organization : "Student Developer",
                Bio = user.Bio,
                Social = new ProfileSocialDto()
            },
            TotalSolved = solvedProblems.Count,
            TotalAttempted = submissions.Count,
            SuccessRate = successRate,
            Streak = new ProfileStreakDto
            {
                CurrentStreak = currentStreak,
                LongestStreak = Math.Max(longestStreak, currentStreak),
                TotalActiveDays = totalActiveDays,
                TotalSubmissionsLastYear = totalLastYear
            },
            DifficultyBreakdown = new DifficultyCountDto
            {
                Easy = easySolved,
                Medium = medSolved,
                Hard = hardSolved
            },
            DifficultyTotals = new DifficultyCountDto
            {
                Easy = totalEasy > 0 ? totalEasy : 10,
                Medium = totalMed > 0 ? totalMed : 20,
                Hard = totalHard > 0 ? totalHard : 10
            },
            LanguageStats = languageStats,
            TopicStats = topicStats,
            ActivityGrid = activityGrid,
            RecentAccepted = recentAccepted
        };
    }
}
