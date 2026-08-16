using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Codify.Infrastructure.Repositories;

public class ContestRepository(CodifyDbContext db) : IContestRepository
{
    public async Task<List<Contest>> GetAllForInstructorAsync(Guid instructorId) =>
        await db.Contests
            .Include(c => c.CreatedByInstructor)
            .Include(c => c.ContestProblems)
                .ThenInclude(cp => cp.Problem)
            .Include(c => c.ContestParticipants)
                .ThenInclude(cp => cp.Student)
            .AsSplitQuery()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

    public async Task<List<Contest>> GetAllForStudentAsync(Guid studentId) =>
        await db.Contests
            .Include(c => c.CreatedByInstructor)
            .Include(c => c.ContestProblems)
                .ThenInclude(cp => cp.Problem)
            .Include(c => c.ContestParticipants)
                .ThenInclude(cp => cp.Student)
            .AsSplitQuery()
            .OrderByDescending(c => c.StartAt)
            .ToListAsync();

    public async Task<Contest?> GetByIdWithDetailsAsync(Guid contestId) =>
        await db.Contests
            .Include(c => c.CreatedByInstructor)
            .Include(c => c.ContestProblems)
                .ThenInclude(cp => cp.Problem)
            .Include(c => c.ContestParticipants)
                .ThenInclude(cp => cp.Student)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == contestId);

    public async Task<List<ContestParticipant>> GetParticipantsByContestIdAsync(Guid contestId) =>
        await db.ContestParticipants
            .Where(cp => cp.ContestId == contestId)
            .Include(cp => cp.Student)
            .Include(cp => cp.Contest)
            .OrderByDescending(cp => cp.Score)
            .ThenByDescending(cp => cp.ProblemsSolved)
            .ThenBy(cp => cp.FinishedAt)
            .ToListAsync();

    public async Task<List<ContestParticipant>> GetStudentContestHistoryAsync(Guid studentId) =>
        await db.ContestParticipants
            .Where(cp => cp.StudentId == studentId)
            .Include(cp => cp.Contest)
                .ThenInclude(c => c.CreatedByInstructor)
            .Include(cp => cp.Contest)
                .ThenInclude(c => c.ContestProblems)
                    .ThenInclude(p => p.Problem)
            .Include(cp => cp.Student)
            .AsSplitQuery()
            .OrderBy(cp => cp.FinishedAt ?? cp.JoinedAt)
            .ToListAsync();

    public async Task<ContestParticipant?> GetParticipantAsync(Guid contestId, Guid studentId) =>
        await db.ContestParticipants
            .Include(cp => cp.Student)
            .Include(cp => cp.Contest)
            .FirstOrDefaultAsync(cp => cp.ContestId == contestId && cp.StudentId == studentId);

    public async Task AddAsync(Contest contest) =>
        await db.Contests.AddAsync(contest);

    public async Task AddParticipantAsync(ContestParticipant participant) =>
        await db.ContestParticipants.AddAsync(participant);

    public async Task SaveChangesAsync() =>
        await db.SaveChangesAsync();
}
