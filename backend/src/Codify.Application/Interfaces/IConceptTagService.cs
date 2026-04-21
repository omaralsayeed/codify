using Codify.Application.DTOs.Tags;

namespace Codify.Application.Interfaces;

public interface IConceptTagService
{
    Task<IEnumerable<ConceptTagResponse>> GetAllAsync();
}
