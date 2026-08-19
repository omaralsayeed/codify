using Codify.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Codify.Infrastructure.Persistence;

public class CodifyDbContext(DbContextOptions<CodifyDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Problem> Problems => Set<Problem>();
    public DbSet<ConceptTag> ConceptTags => Set<ConceptTag>();
    public DbSet<ProblemTag> ProblemTags => Set<ProblemTag>();
    public DbSet<TestCase> TestCases => Set<TestCase>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<SubmissionResult> SubmissionResults => Set<SubmissionResult>();
    public DbSet<HintLog> HintLogs => Set<HintLog>();
    public DbSet<PerformanceProfile> PerformanceProfiles => Set<PerformanceProfile>();
    public DbSet<FeedbackRecord> FeedbackRecords => Set<FeedbackRecord>();
    public DbSet<TestCaseResult> TestCaseResults => Set<TestCaseResult>();
    public DbSet<Contest> Contests => Set<Contest>();
    public DbSet<ContestProblem> ContestProblems => Set<ContestProblem>();
    public DbSet<ContestParticipant> ContestParticipants => Set<ContestParticipant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CodifyDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
