using Codify.Application.DTOs.Tags;
using Codify.Application.Interfaces;

namespace Codify.Application.Services;

public class ConceptTagService(IConceptTagRepository tagRepo) : IConceptTagService
{
    public async Task<IEnumerable<ConceptTagResponse>> GetAllAsync()
    {
        var tags = await tagRepo.GetAllAsync();
        return tags.Select(t => new ConceptTagResponse
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description
        });
    }
}
