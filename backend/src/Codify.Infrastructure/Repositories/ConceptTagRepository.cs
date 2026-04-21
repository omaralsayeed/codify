using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Codify.Infrastructure.Repositories;

public class ConceptTagRepository(CodifyDbContext db) : IConceptTagRepository
{
    public async Task<IEnumerable<ConceptTag>> GetAllAsync() =>
        await db.ConceptTags.OrderBy(t => t.Name).ToListAsync();

    public async Task<IEnumerable<ConceptTag>> GetByIdsAsync(IEnumerable<Guid> ids) =>
        await db.ConceptTags.Where(t => ids.Contains(t.Id)).ToListAsync();
}
