using Codify.Application.Agents;
using Codify.Application.Interfaces;
using Codify.Application.Services;
using Codify.Domain.Entities;
using Codify.Domain.Enums;
using Codify.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Codify.Tests.Application;

/// <summary>
/// Tests for the TaggingService orchestrator: tags untagged problems, skips
/// already-tagged problems, runs the automatic scan, and delegates the
/// on-progress user-tag refresh to the performance service.
/// </summary>
public class TaggingServiceTests
{
    private readonly ITaggingAgent _taggingAgent = Substitute.For<ITaggingAgent>();
    private readonly IProblemRepository _problemRepo = Substitute.For<IProblemRepository>();
    private readonly IConceptTagRepository _conceptTagRepo = Substitute.For<IConceptTagRepository>();
    private readonly IPerformanceService _performanceService = Substitute.For<IPerformanceService>();
    private readonly TaggingService _sut;

    public TaggingServiceTests()
    {
        _sut = new TaggingService(
            _taggingAgent, _problemRepo, _conceptTagRepo, _performanceService,
            NullLogger<TaggingService>.Instance);
    }

    private static Problem MakeProblem() =>
        Problem.Create("Two Sum", "Find two numbers.", Difficulty.Easy, "", "[]");

    private static ConceptTag MakeTag(string name) => ConceptTag.Create(name, "desc");

    [Fact]
    public async Task TagProblem_ShouldThrowNotFound_WhenProblemMissing()
    {
        _problemRepo.GetByIdWithDetailsAsync(Arg.Any<Guid>()).Returns((Problem?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.TagProblemAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task TagProblem_ShouldSkip_WhenAlreadyTagged()
    {
        var problem = MakeProblem();
        problem.ProblemTags.Add(ProblemTag.Create(problem.Id, Guid.NewGuid()));
        _problemRepo.GetByIdWithDetailsAsync(problem.Id).Returns(problem);

        var result = await _sut.TagProblemAsync(problem.Id);

        Assert.True(result.AlreadyTagged);
        await _taggingAgent.DidNotReceive().ClassifyProblemTagsAsync(Arg.Any<TaggingAgentInput>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TagProblem_ShouldClassifyAndApplyTags_WhenUntagged()
    {
        var problem = MakeProblem();
        var graphTag = MakeTag("Graphs");

        _problemRepo.GetByIdWithDetailsAsync(problem.Id).Returns(problem);
        _conceptTagRepo.GetAllAsync().Returns(new[] { graphTag, MakeTag("Trees") });
        _taggingAgent.ClassifyProblemTagsAsync(Arg.Any<TaggingAgentInput>(), Arg.Any<CancellationToken>())
            .Returns(new TagClassificationResult { AssignedTags = ["Graphs"], Confidence = 0.9, Reasoning = "graph" });

        var result = await _sut.TagProblemAsync(problem.Id);

        Assert.False(result.AlreadyTagged);
        Assert.Contains("Graphs", result.AssignedTags);
        await _conceptTagRepo.Received(1).AddProblemTagAsync(Arg.Is<ProblemTag>(pt => pt.ConceptTagId == graphTag.Id));
        await _conceptTagRepo.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task TagProblem_ShouldApplyNothing_WhenAgentAssignsNoTags()
    {
        var problem = MakeProblem();
        _problemRepo.GetByIdWithDetailsAsync(problem.Id).Returns(problem);
        _conceptTagRepo.GetAllAsync().Returns(new[] { MakeTag("Graphs") });
        _taggingAgent.ClassifyProblemTagsAsync(Arg.Any<TaggingAgentInput>(), Arg.Any<CancellationToken>())
            .Returns(new TagClassificationResult { AssignedTags = [] });

        var result = await _sut.TagProblemAsync(problem.Id);

        Assert.Empty(result.AssignedTags);
        await _conceptTagRepo.DidNotReceive().AddProblemTagAsync(Arg.Any<ProblemTag>());
    }

    [Fact]
    public async Task TagAllUntagged_ShouldTagEachUntaggedProblem()
    {
        var p1 = MakeProblem();
        var p2 = MakeProblem();
        var graphTag = MakeTag("Graphs");

        _problemRepo.GetUntaggedProblemsAsync().Returns(new List<Problem> { p1, p2 });
        _conceptTagRepo.GetAllAsync().Returns(new[] { graphTag });
        _taggingAgent.ClassifyProblemTagsAsync(Arg.Any<TaggingAgentInput>(), Arg.Any<CancellationToken>())
            .Returns(new TagClassificationResult { AssignedTags = ["Graphs"], Confidence = 0.8 });

        var scan = await _sut.TagAllUntaggedProblemsAsync();

        Assert.Equal(2, scan.UntaggedFound);
        Assert.Equal(2, scan.Tagged);
        await _conceptTagRepo.Received(2).AddProblemTagAsync(Arg.Any<ProblemTag>());
    }

    [Fact]
    public async Task UpdateUserTagsOnProgress_ShouldDelegateToPerformanceService()
    {
        var userId = Guid.NewGuid();

        await _sut.UpdateUserTagsOnProgressAsync(userId);

        await _performanceService.Received(1).UpdateAfterSubmissionAsync(userId);
    }
}
