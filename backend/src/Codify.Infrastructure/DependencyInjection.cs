using Codify.Application.Agents;
using Codify.Application.Interfaces;
using Codify.Application.Services;
using Codify.Infrastructure.AI;
using Codify.Infrastructure.Auth;
using Codify.Infrastructure.BackgroundJobs;
using Codify.Infrastructure.Judge0;
using Codify.Infrastructure.Persistence;
using Codify.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Codify.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<CodifyDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProblemRepository, ProblemRepository>();
        services.AddScoped<IConceptTagRepository, ConceptTagRepository>();
        services.AddScoped<ISubmissionRepository, SubmissionRepository>();
        services.AddScoped<IHintRepository, HintRepository>();
        services.AddScoped<IFeedbackRepository, FeedbackRepository>();
        services.AddScoped<IPerformanceRepository, PerformanceRepository>();
        services.AddScoped<ITestCaseRepository, TestCaseRepository>();
        services.AddScoped<ITestCaseResultRepository, TestCaseResultRepository>();

        // Auth
        services.AddScoped<IJwtService, JwtService>();

        // Application Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IProblemService, ProblemService>();
        services.AddScoped<IConceptTagService, ConceptTagService>();
        services.AddScoped<ISubmissionService, SubmissionService>();
        services.AddScoped<ITestCaseService, TestCaseService>();
        services.AddScoped<IExecutionService, ExecutionService>();
        services.AddScoped<IQuickRunService, QuickRunService>();
        services.AddScoped<IQuickRunWithTestsService, QuickRunWithTestsService>();
        services.AddScoped<IAiHintService, AiHintService>();
        services.AddScoped<IPerformanceService, PerformanceService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IJudgeEvaluationService, JudgeEvaluationService>();

        // Judge0 (code evaluation)
        services.Configure<Judge0Options>(configuration.GetSection(Judge0Options.SectionName));
        services.AddHttpClient<IJudge0Client, Judge0Client>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<Judge0Options>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(30);

            if (!string.IsNullOrWhiteSpace(options.ApiKey))
                client.DefaultRequestHeaders.Add("X-RapidAPI-Key", options.ApiKey);
            if (!string.IsNullOrWhiteSpace(options.ApiHost))
                client.DefaultRequestHeaders.Add("X-RapidAPI-Host", options.ApiHost);
        });

        // Background evaluation queue (Channel-based) + its hosted worker
        services.AddSingleton<ISubmissionEvaluationQueue, SubmissionEvaluationQueue>();
        services.AddHostedService<SubmissionEvaluationBackgroundService>();

        // AI
        services.Configure<OpenAiOptions>(options =>
        {
            options.ApiKey = configuration[$"{OpenAiOptions.SectionName}:ApiKey"] ?? string.Empty;
            options.Model  = configuration[$"{OpenAiOptions.SectionName}:Model"] ?? OpenAiOptions.DefaultModel;
        });
        services.AddSingleton<ILLMClient, OpenAiChatClient>();
        services.AddSingleton<IPromptLoader, PromptLoader>();
        services.AddScoped<ITutorAgent, TutorAgent>();
        services.AddScoped<ICodeCheckerAgent, CodeCheckerAgent>();

        return services;
    }
}
