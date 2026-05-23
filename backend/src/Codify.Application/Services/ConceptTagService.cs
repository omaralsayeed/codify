using Codify.Application.DTOs.Tags;
using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Domain.Exceptions;

namespace Codify.Application.Services;

// The service is the "brain" of the feature. It:
//   1. Validates business rules (e.g. duplicate names)
//   2. Delegates data access to the repository
//   3. Maps domain entities to DTOs (the shape we send back to the client)
public class ConceptTagService(
    IConceptTagRepository tagRepo,
    IProblemRepository problemRepo) : IConceptTagService
{
    // ── ConceptTag CRUD ─────────────────────────────────────────────────

    public async Task<IEnumerable<ConceptTagResponse>> GetAllAsync()
    {
        var tags = await tagRepo.GetAllAsync();
        return tags.Select(MapToResponse);
    }

    public async Task<ConceptTagResponse> GetByIdAsync(Guid id)
    {
        // NotFoundException is caught by ExceptionMiddleware and turned into a 404 response
        var tag = await tagRepo.GetByIdAsync(id)
            ?? throw new NotFoundException($"ConceptTag {id} not found.");

        return MapToResponse(tag);
    }

    public async Task<ConceptTagResponse> CreateAsync(CreateConceptTagRequest request)
    {
        // Business rule: tag names must be unique (slug is derived from name)
        var existing = await tagRepo.GetByNameAsync(request.Name);
        if (existing is not null)
            throw new ValidationException($"A tag with the name '{request.Name}' already exists.");

        // The entity's static factory method handles ID generation and timestamp setting
        var tag = ConceptTag.Create(request.Name, request.Description);
        await tagRepo.AddAsync(tag);
        await tagRepo.SaveChangesAsync();

        return MapToResponse(tag);
    }

    public async Task<ConceptTagResponse> UpdateAsync(Guid id, UpdateConceptTagRequest request)
    {
        var tag = await tagRepo.GetByIdAsync(id)
            ?? throw new NotFoundException($"ConceptTag {id} not found.");

        // Check for name collision, but allow the tag to keep its own name
        var existing = await tagRepo.GetByNameAsync(request.Name);
        if (existing is not null && existing.Id != id)
            throw new ValidationException($"A tag with the name '{request.Name}' already exists.");

        // Calling Update() on the entity respects the private setters pattern
        tag.Update(request.Name, request.Description);
        await tagRepo.SaveChangesAsync(); // EF Core change tracking detects the mutation automatically

        return MapToResponse(tag);
    }

    public async Task DeleteAsync(Guid id)
    {
        var tag = await tagRepo.GetByIdAsync(id)
            ?? throw new NotFoundException($"ConceptTag {id} not found.");

        // Soft delete: sets IsDeleted = true, which the global query filter hides automatically.
        // The tag row stays in the DB — this is important for historical data integrity.
        tag.SoftDelete();
        await tagRepo.SaveChangesAsync();
    }

    // ── ProblemTag operations ────────────────────────────────────────────

    public async Task<IEnumerable<ConceptTagResponse>> GetTagsForProblemAsync(Guid problemId)
    {
        var problemTags = await tagRepo.GetTagsByProblemIdAsync(problemId);
        return problemTags.Select(pt => MapToResponse(pt.ConceptTag));
    }

    public async Task AddTagToProblemAsync(Guid problemId, Guid conceptTagId)
    {
        // Validate both sides of the relationship exist
        var problem = await problemRepo.GetByIdWithDetailsAsync(problemId)
            ?? throw new NotFoundException($"Problem {problemId} not found.");

        var tag = await tagRepo.GetByIdAsync(conceptTagId)
            ?? throw new NotFoundException($"ConceptTag {conceptTagId} not found.");

        // Prevent duplicate join entries
        var existing = await tagRepo.GetProblemTagAsync(problemId, conceptTagId);
        if (existing is not null)
            throw new ValidationException("This tag is already applied to this problem.");

        // ProblemTag is a join table entity — it just holds two foreign keys
        var problemTag = ProblemTag.Create(problemId, conceptTagId);
        await tagRepo.AddProblemTagAsync(problemTag);
        await tagRepo.SaveChangesAsync();
    }

    public async Task RemoveTagFromProblemAsync(Guid problemId, Guid conceptTagId)
    {
        var problemTag = await tagRepo.GetProblemTagAsync(problemId, conceptTagId)
            ?? throw new NotFoundException("This tag is not applied to this problem.");

        // Hard delete the join row — this is fine, ProblemTag rows have no historical significance
        tagRepo.RemoveProblemTag(problemTag);
        await tagRepo.SaveChangesAsync();
    }

    // ── Private helper ───────────────────────────────────────────────────

    // Static mapper: converts a domain entity → a DTO safe to send over HTTP
    private static ConceptTagResponse MapToResponse(ConceptTag tag) => new()
    {
        Id = tag.Id,
        Name = tag.Name,
        Slug = tag.Slug,
        Description = tag.Description,
        CreatedAt = tag.CreatedAt
    };
}
