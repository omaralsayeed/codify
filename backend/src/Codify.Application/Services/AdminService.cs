using Codify.Application.DTOs.Admin;
using Codify.Application.Interfaces;
using Codify.Domain.Enums;
using Codify.Domain.Exceptions;

namespace Codify.Application.Services;

public class AdminService(
    IUserRepository userRepo,
    IProblemRepository problemRepo,
    IFeedbackRepository feedbackRepo,
    IContestRepository contestRepo) : IAdminService
{
    public async Task<IReadOnlyList<PendingInstructorResponse>> GetPendingInstructorsAsync()
    {
        var instructors = await userRepo.GetPendingInstructorsAsync();

        return instructors.Select(u => new PendingInstructorResponse
        {
            UserId       = u.Id,
            FullName     = u.FullName,
            Email        = u.Email,
            Organization = u.Organization,
            RegisteredAt = u.CreatedAt
        }).ToList();
    }

    public async Task<ApproveInstructorResponse> ApproveInstructorAsync(Guid instructorId, Guid adminId)
    {
        var instructor = await userRepo.GetByIdAsync(instructorId)
            ?? throw new NotFoundException("Instructor not found.");

        if (instructor.Role != UserRole.Instructor)
            throw new ValidationException("The specified user is not an instructor.");

        if (instructor.Status == UserStatus.Active)
            throw new ValidationException("This instructor account is already active.");

        instructor.Approve(adminId);
        await userRepo.SaveChangesAsync();

        return new ApproveInstructorResponse
        {
            UserId     = instructor.Id,
            FullName   = instructor.FullName,
            Email      = instructor.Email,
            ApprovedAt = instructor.ReviewedAt!.Value
        };
    }

    public async Task<AdminStatsResponse> GetStatsAsync()
    {
        var users = await userRepo.GetAllUsersAsync();
        var problems = await problemRepo.GetAllActiveWithTagsAsync();
        var contests = await contestRepo.GetAllForInstructorAsync(Guid.Empty);
        var flags = await feedbackRepo.GetAiGeneratedFlagsAsync();

        var allSubmissions = users.SelectMany(u => u.Submissions).ToList();
        int totalSubmissions = allSubmissions.Count;
        int acceptedSubmissions = allSubmissions.Count(s => s.Status == SubmissionStatus.Accepted);
        double passRate = totalSubmissions > 0
            ? Math.Round((double)acceptedSubmissions / totalSubmissions * 100, 1)
            : 0;

        return new AdminStatsResponse
        {
            TotalUsers = users.Count,
            ActiveStudents = users.Count(u => u.Role == UserRole.Student && u.Status == UserStatus.Active),
            PendingInstructors = users.Count(u => u.Role == UserRole.Instructor && u.Status == UserStatus.Pending),
            ActiveInstructors = users.Count(u => u.Role == UserRole.Instructor && u.Status == UserStatus.Active),
            TotalProblems = problems.Count,
            TotalSubmissions = totalSubmissions,
            PassRatePercent = passRate,
            TotalContests = contests.Count,
            AiFlagsCount = flags.Count
        };
    }

    public async Task<IReadOnlyList<AdminUserListItemResponse>> GetAllUsersAsync()
    {
        var users = await userRepo.GetAllUsersAsync();

        return users.Select(u => new AdminUserListItemResponse
        {
            Id = u.Id,
            Name = u.FullName,
            Email = u.Email,
            Role = u.Role.ToString().ToLower(),
            Status = u.Status.ToString().ToLower(),
            Organization = u.Organization,
            SolvedProblems = u.Submissions.Where(s => s.Status == SubmissionStatus.Accepted).Select(s => s.ProblemId).Distinct().Count(),
            TotalSubmissions = u.Submissions.Count,
            CreatedAt = u.CreatedAt,
            LastLoginAt = u.LastLoginAt
        }).ToList();
    }

    public async Task<AdminUserDetailResponse> GetUserDetailAsync(Guid id)
    {
        var user = await userRepo.GetUserWithProfileDataAsync(id.ToString())
            ?? throw new NotFoundException($"User {id} not found.");

        var submissions = user.Submissions.ToList();
        var accepted = submissions.Where(s => s.Status == SubmissionStatus.Accepted).ToList();
        double successRate = submissions.Count > 0
            ? Math.Round((double)accepted.Count / submissions.Count * 100, 1)
            : 0;

        var recentSubmissions = submissions
            .OrderByDescending(s => s.SubmittedAt)
            .Take(15)
            .Select(s => new AdminUserSubmissionDto
            {
                Id = s.Id,
                ProblemId = s.ProblemId,
                ProblemTitle = s.Problem?.Title ?? "Problem",
                Difficulty = s.Problem?.Difficulty.ToString() ?? "Medium",
                Language = s.Language.ToString(),
                Status = s.Status.ToString(),
                ExecutionTimeMs = s.ExecutionTimeMs,
                SubmittedAt = s.SubmittedAt
            })
            .ToList();

        return new AdminUserDetailResponse
        {
            Id = user.Id,
            Name = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString().ToLower(),
            Status = user.Status.ToString().ToLower(),
            Organization = user.Organization,
            Bio = user.Bio,
            Rating = user.Rating,
            SolvedProblems = accepted.Select(s => s.ProblemId).Distinct().Count(),
            TotalSubmissions = submissions.Count,
            SuccessRate = successRate,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            RecentSubmissions = recentSubmissions
        };
    }

    public async Task<bool> UpdateUserStatusAsync(Guid id, UserStatus newStatus)
    {
        var user = await userRepo.GetByIdAsync(id)
            ?? throw new NotFoundException($"User {id} not found.");

        // If activating an instructor via status change
        if (user.Role == UserRole.Instructor && newStatus == UserStatus.Active && user.Status == UserStatus.Pending)
        {
            user.Approve(Guid.Empty);
        }
        else
        {
            // Update through private setter / reflection or helper method
            typeof(Codify.Domain.Entities.User)
                .GetProperty(nameof(user.Status))?
                .SetValue(user, newStatus);
        }

        await userRepo.SaveChangesAsync();
        return true;
    }
}
