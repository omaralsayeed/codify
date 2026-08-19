using Codify.Application.DTOs.Contests;
using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Domain.Enums;
using Codify.Domain.Exceptions;

namespace Codify.Application.Services;

public class ContestService(
    IContestRepository contestRepo,
    IProblemRepository problemRepo,
    IUserRepository userRepo) : IContestService
{
    public async Task<List<ContestDto>> GetInstructorContestsAsync(Guid instructorId)
    {
        var contests = await contestRepo.GetAllForInstructorAsync(instructorId);
        return contests.Select(MapToContestDto).ToList();
    }

    public async Task<ContestDto> GetContestByIdAsync(Guid contestId)
    {
        var contest = await contestRepo.GetByIdWithDetailsAsync(contestId)
            ?? throw new NotFoundException($"Contest {contestId} not found.");

        return MapToContestDto(contest);
    }

    public async Task<ContestDto> CreateContestAsync(Guid instructorId, CreateContestRequest request)
    {
        if (request.ProblemIds.Count == 0)
            throw new ValidationException("A contest must contain at least one problem.");

        if (request.EndAt <= request.StartAt)
            throw new ValidationException("End date must be after start date.");

        if (request.AssignedStudentIds.Count == 0)
        {
            var allUsers = await userRepo.GetAllUsersAsync();
            request.AssignedStudentIds = allUsers.Where(u => u.Role == UserRole.Student).Select(u => u.Id).ToList();
        }

        var instructor = await userRepo.GetByIdAsync(instructorId)
            ?? throw new NotFoundException($"Instructor {instructorId} not found.");

        var contest = new Contest
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            CreatedByInstructorId = instructorId,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            Status = ContestStatus.Upcoming,
            CreatedAt = DateTime.UtcNow
        };

        // Determine current status
        contest.Status = contest.CalculateCurrentStatus();

        // Add problems with ordering and points
        int order = 1;
        foreach (var problemId in request.ProblemIds.Distinct())
        {
            contest.ContestProblems.Add(new ContestProblem
            {
                ContestId = contest.Id,
                ProblemId = problemId,
                Order = order++,
                Points = 100
            });
        }

        // Add assigned participants
        foreach (var studentId in request.AssignedStudentIds.Distinct())
        {
            contest.ContestParticipants.Add(new ContestParticipant
            {
                ContestId = contest.Id,
                StudentId = studentId,
                Score = 0,
                ProblemsSolved = 0,
                Accuracy = 0,
                Rank = 0,
                JoinedAt = DateTime.UtcNow
            });
        }

        await contestRepo.AddAsync(contest);
        await contestRepo.SaveChangesAsync();

        // Reload with details for DTO mapping
        var created = await contestRepo.GetByIdWithDetailsAsync(contest.Id);
        return MapToContestDto(created ?? contest);
    }

    public async Task<List<ContestResultDto>> GetContestResultsAsync(Guid contestId)
    {
        var participants = await contestRepo.GetParticipantsByContestIdAsync(contestId);
        var contest = await contestRepo.GetByIdWithDetailsAsync(contestId);
        int totalProblems = contest?.ContestProblems.Count ?? 0;

        var results = new List<ContestResultDto>();
        int rank = 1;

        foreach (var p in participants)
        {
            results.Add(new ContestResultDto
            {
                ContestId = p.ContestId,
                ContestTitle = contest?.Title ?? string.Empty,
                StudentId = p.StudentId,
                StudentName = p.Student?.FullName ?? "Unknown",
                Rank = rank++,
                Score = p.Score,
                ProblemsSolved = p.ProblemsSolved,
                TotalProblems = totalProblems,
                Accuracy = p.Accuracy,
                FinishedAt = p.FinishedAt
            });
        }

        return results;
    }

    public async Task<List<ContestResultDto>> GetStudentContestHistoryAsync(Guid studentId)
    {
        var history = await contestRepo.GetStudentContestHistoryAsync(studentId);
        var results = new List<ContestResultDto>();

        foreach (var p in history)
        {
            int totalProblems = p.Contest?.ContestProblems.Count ?? 0;
            results.Add(new ContestResultDto
            {
                ContestId = p.ContestId,
                ContestTitle = p.Contest?.Title ?? string.Empty,
                StudentId = p.StudentId,
                StudentName = p.Student?.FullName ?? string.Empty,
                Rank = p.Rank > 0 ? p.Rank : 1,
                Score = p.Score,
                ProblemsSolved = p.ProblemsSolved,
                TotalProblems = totalProblems,
                Accuracy = p.Accuracy,
                FinishedAt = p.FinishedAt
            });
        }

        return results;
    }

    public async Task<StudentContestsOverviewDto> GetStudentContestsOverviewAsync(Guid studentId)
    {
        var studentContests = await contestRepo.GetAllForStudentAsync(studentId);

        var live = new List<ContestDto>();
        var upcoming = new List<ContestDto>();
        var past = new List<StudentPastContestDto>();

        foreach (var c in studentContests)
        {
            var status = c.CalculateCurrentStatus();
            var dto = MapToContestDto(c);
            dto.Status = status;

            if (status == ContestStatus.Live)
            {
                live.Add(dto);
            }
            else if (status == ContestStatus.Upcoming)
            {
                upcoming.Add(dto);
            }
            else if (status == ContestStatus.Ended)
            {
                var participant = c.ContestParticipants.FirstOrDefault(cp => cp.StudentId == studentId);
                past.Add(new StudentPastContestDto
                {
                    ContestId = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    InstructorName = c.CreatedByInstructor?.FullName ?? "Instructor",
                    StartAt = c.StartAt,
                    EndAt = c.EndAt,
                    TotalProblems = c.ContestProblems.Count,
                    ProblemsSolved = participant?.ProblemsSolved ?? 0,
                    Score = participant?.Score ?? 0,
                    Rank = participant?.Rank ?? 1,
                    Accuracy = participant?.Accuracy ?? 0,
                    FinishedAt = participant?.FinishedAt,
                    Problems = dto.Problems
                });
            }
        }

        return new StudentContestsOverviewDto
        {
            HasActiveContestNotification = live.Count > 0,
            ActiveContestsCount = live.Count,
            LiveContests = live,
            UpcomingContests = upcoming,
            PastContests = past
        };
    }

    private static ContestDto MapToContestDto(Contest c)
    {
        var status = c.CalculateCurrentStatus();
        return new ContestDto
        {
            Id = c.Id,
            Title = c.Title,
            Description = c.Description,
            CreatedByInstructorId = c.CreatedByInstructorId,
            InstructorName = c.CreatedByInstructor?.FullName ?? string.Empty,
            ProblemIds = c.ContestProblems.Select(cp => cp.ProblemId).ToList(),
            Problems = c.ContestProblems
                .OrderBy(cp => cp.Order)
                .Select(cp => new ContestProblemDetailDto
                {
                    Id = cp.ProblemId,
                    Title = cp.Problem?.Title ?? "Problem",
                    Difficulty = cp.Problem?.Difficulty.ToString() ?? "Medium",
                    Points = cp.Points,
                    Order = cp.Order
                }).ToList(),
            AssignedStudentIds = c.ContestParticipants.Select(cp => cp.StudentId).ToList(),
            StartAt = c.StartAt,
            EndAt = c.EndAt,
            Status = status,
            CreatedAt = c.CreatedAt
        };
    }
}
