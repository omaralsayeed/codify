using Codify.Application.DTOs.Tags;

namespace Codify.Application.Interfaces;

// IConceptTagService is what the controller talks to.
// It knows nothing about databases — that's the repository's job.
public interface IConceptTagService
{
    // ConceptTag CRUD
    Task<IEnumerable<ConceptTagResponse>> GetAllAsync();
    Task<ConceptTagResponse> GetByIdAsync(Guid id);
    Task<ConceptTagResponse> CreateAsync(CreateConceptTagRequest request);
    Task<ConceptTagResponse> UpdateAsync(Guid id, UpdateConceptTagRequest request);
    Task DeleteAsync(Guid id);

    // ProblemTag operations — tagging a problem with a concept
    Task<IEnumerable<ConceptTagResponse>> GetTagsForProblemAsync(Guid problemId);
    Task AddTagToProblemAsync(Guid problemId, Guid conceptTagId);
    Task RemoveTagFromProblemAsync(Guid problemId, Guid conceptTagId);
}
