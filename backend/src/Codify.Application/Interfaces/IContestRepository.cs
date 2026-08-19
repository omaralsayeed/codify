using Codify.Domain.Entities;

namespace Codify.Application.Interfaces;

public interface IContestRepository
{
    Task<List<Contest>> GetAllForInstructorAsync(Guid instructorId);
    Task<List<Contest>> GetAllForStudentAsync(Guid studentId);
    Task<Contest?> GetByIdWithDetailsAsync(Guid contestId);
    Task<List<ContestParticipant>> GetParticipantsByContestIdAsync(Guid contestId);
    Task<List<ContestParticipant>> GetStudentContestHistoryAsync(Guid studentId);
    Task<ContestParticipant?> GetParticipantAsync(Guid contestId, Guid studentId);
    Task AddAsync(Contest contest);
    Task AddParticipantAsync(ContestParticipant participant);
    Task SaveChangesAsync();
}
