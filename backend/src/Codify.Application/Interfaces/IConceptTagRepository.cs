using Codify.Domain.Entities;

namespace Codify.Application.Interfaces;

public interface IConceptTagRepository
{
    Task<IEnumerable<ConceptTag>> GetAllAsync();
    Task<IEnumerable<ConceptTag>> GetByIdsAsync(IEnumerable<Guid> ids);
}
