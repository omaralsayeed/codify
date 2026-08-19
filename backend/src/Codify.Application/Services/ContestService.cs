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
        return contests.Select(c => MapToContestDto(c)).ToList();
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

        // Collect student participants by emails or student IDs
        var assignedStudents = new List<User>();

        if (request.StudentEmails.Count > 0)
        {
            var matchedStudents = await userRepo.GetStudentsByEmailsAsync(request.StudentEmails);
            assignedStudents.AddRange(matchedStudents);

            // If some emails are not found in the DB, we can still record them or warn if needed
            var matchedEmails = matchedStudents.Select(s => s.Email.ToLower()).ToHashSet();
            var missingEmails = request.StudentEmails
                .Where(e => !string.IsNullOrWhiteSpace(e) && !matchedEmails.Contains(e.Trim().ToLower()))
                .ToList();

            if (missingEmails.Count > 0 && assignedStudents.Count == 0)
            {
                throw new ValidationException($"No registered student accounts found for: {string.Join(", ", missingEmails)}");
            }
        }

        if (request.AssignedStudentIds.Count > 0)
        {
            foreach (var sid in request.AssignedStudentIds.Distinct())
            {
                if (assignedStudents.All(s => s.Id != sid))
                {
                    var user = await userRepo.GetByIdAsync(sid);
                    if (user != null && user.Role == UserRole.Student)
                    {
                        assignedStudents.Add(user);
                    }
                }
            }
        }

        // If no students assigned explicitly, assign existing students of this instructor
        if (assignedStudents.Count == 0 && request.StudentEmails.Count == 0 && request.AssignedStudentIds.Count == 0)
        {
            var instructorStudents = await userRepo.GetStudentsForInstructorAsync(instructorId);
            assignedStudents.AddRange(instructorStudents);
        }

        // Add participants as Pending invitations and link to InstructorStudents
        foreach (var student in assignedStudents.DistinctBy(s => s.Id))
        {
            contest.ContestParticipants.Add(new ContestParticipant
            {
                ContestId = contest.Id,
                StudentId = student.Id,
                InvitedEmail = student.Email,
                InvitationStatus = InvitationStatus.Pending,
                Score = 0,
                ProblemsSolved = 0,
                Accuracy = 0,
                Rank = 0,
                JoinedAt = DateTime.UtcNow
            });

            // Ensure the student is registered under this instructor
            await userRepo.EnsureInstructorStudentEnrolledAsync(instructorId, student.Id);
        }

        await contestRepo.AddAsync(contest);
        await contestRepo.SaveChangesAsync();

        // Reload with details for DTO mapping
        var created = await contestRepo.GetByIdWithDetailsAsync(contest.Id);
        return MapToContestDto(created ?? contest);
    }

    public async Task RespondToInvitationAsync(Guid contestId, Guid studentId, bool accept)
    {
        var participant = await contestRepo.GetParticipantAsync(contestId, studentId)
            ?? throw new NotFoundException($"Contest invitation for student {studentId} not found.");

        participant.InvitationStatus = accept ? InvitationStatus.Accepted : InvitationStatus.Declined;
        participant.RespondedAt = DateTime.UtcNow;

        if (accept && participant.Contest != null)
        {
            await userRepo.EnsureInstructorStudentEnrolledAsync(participant.Contest.CreatedByInstructorId, studentId);
        }

        await contestRepo.SaveChangesAsync();
    }

    public async Task<List<ContestResultDto>> GetContestResultsAsync(Guid contestId)
    {
        var participants = await contestRepo.GetParticipantsByContestIdAsync(contestId);
        var contest = await contestRepo.GetByIdWithDetailsAsync(contestId);
        int totalProblems = contest?.ContestProblems.Count ?? 0;

        var results = new List<ContestResultDto>();
        int rank = 1;

        // Only accepted participants appear in ranked results
        var accepted = participants.Where(p => p.InvitationStatus == InvitationStatus.Accepted).ToList();

        foreach (var p in accepted)
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

        foreach (var p in history.Where(x => x.InvitationStatus == InvitationStatus.Accepted))
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

        var pending = new List<ContestDto>();
        var live = new List<ContestDto>();
        var upcoming = new List<ContestDto>();
        var past = new List<StudentPastContestDto>();

        foreach (var c in studentContests)
        {
            var participant = c.ContestParticipants.FirstOrDefault(cp => cp.StudentId == studentId);
            if (participant == null) continue;

            var status = c.CalculateCurrentStatus();
            var dto = MapToContestDto(c, studentId);
            dto.Status = status;
            dto.MyInvitationStatus = participant.InvitationStatus;

            // 1. Pending Invitation
            if (participant.InvitationStatus == InvitationStatus.Pending)
            {
                pending.Add(dto);
                continue;
            }

            // 2. Declined Invitation -> skip from active list
            if (participant.InvitationStatus == InvitationStatus.Declined)
            {
                continue;
            }

            // 3. Accepted: Live, Upcoming, or Ended
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
                past.Add(new StudentPastContestDto
                {
                    ContestId = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    InstructorName = c.CreatedByInstructor?.FullName ?? "Instructor",
                    StartAt = c.StartAt,
                    EndAt = c.EndAt,
                    TotalProblems = c.ContestProblems.Count,
                    ProblemsSolved = participant.ProblemsSolved,
                    Score = participant.Score,
                    Rank = participant.Rank > 0 ? participant.Rank : 1,
                    Accuracy = participant.Accuracy,
                    FinishedAt = participant.FinishedAt,
                    Problems = dto.Problems
                });
            }
        }

        return new StudentContestsOverviewDto
        {
            HasActiveContestNotification = live.Count > 0 || pending.Count > 0,
            ActiveContestsCount = live.Count,
            PendingInvitations = pending,
            LiveContests = live,
            UpcomingContests = upcoming,
            PastContests = past
        };
    }

    private static ContestDto MapToContestDto(Contest c, Guid? studentId = null)
    {
        var status = c.CalculateCurrentStatus();
        var myParticipant = studentId.HasValue ? c.ContestParticipants.FirstOrDefault(cp => cp.StudentId == studentId.Value) : null;

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
            Participants = c.ContestParticipants
                .Select(cp => new ContestParticipantSummaryDto
                {
                    StudentId = cp.StudentId,
                    StudentName = cp.Student?.FullName ?? cp.InvitedEmail ?? "Student",
                    StudentEmail = cp.Student?.Email ?? cp.InvitedEmail ?? string.Empty,
                    InvitationStatus = cp.InvitationStatus,
                    RespondedAt = cp.RespondedAt,
                    Score = cp.Score,
                    ProblemsSolved = cp.ProblemsSolved,
                    Accuracy = cp.Accuracy,
                    Rank = cp.Rank
                }).ToList(),
            MyInvitationStatus = myParticipant?.InvitationStatus,
            StartAt = c.StartAt,
            EndAt = c.EndAt,
            Status = status,
            CreatedAt = c.CreatedAt
        };
    }
}
