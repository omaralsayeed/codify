using Codify.Application.Agents;
using Codify.Application.Interfaces;
using Codify.Application.Services;
using Codify.Infrastructure.AI;
using Codify.Infrastructure.Auth;
using Codify.Infrastructure.Execution;
using Codify.Infrastructure.Persistence;
using Codify.Infrastructure.Repositories;
using Codify.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddScoped<IHintLogRepository, HintLogRepository>();
        services.AddScoped<IPerformanceProfileRepository, PerformanceProfileRepository>();
        services.AddScoped<IKnowledgeBaseSearchService, KnowledgeBaseSearchService>();

        // Auth
        services.AddScoped<IJwtService, JwtService>();

        // Application Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProblemService, ProblemService>();
        services.AddScoped<IConceptTagService, ConceptTagService>();
        services.AddScoped<ISubmissionService, SubmissionService>();
        services.AddScoped<IAiHintService, AiHintService>();
        services.AddScoped<ICodeAnalysisService, CodeAnalysisService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();

        // Execution - Judge0 sandboxed execution
        services.Configure<Judge0Options>(options =>
        {
            options.BaseUrl = configuration[$"{Judge0Options.SectionName}:BaseUrl"] ?? "http://localhost:2358";
            options.AuthToken = configuration[$"{Judge0Options.SectionName}:AuthToken"] ?? string.Empty;
            options.TimeoutMs = int.TryParse(configuration[$"{Judge0Options.SectionName}:TimeoutMs"], out var t) ? t : 30000;
        });
        services.AddHttpClient<Judge0ExecutionService>((sp, client) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Judge0Options>>().Value;
            client.BaseAddress = new Uri(opts.BaseUrl);
            client.Timeout = TimeSpan.FromMilliseconds(opts.TimeoutMs);
        });
        services.AddScoped<IExecutionService>(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(Judge0ExecutionService));
            var problemRepo = sp.GetRequiredService<IProblemRepository>();
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Judge0Options>>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Judge0ExecutionService>>();
            return new Judge0ExecutionService(httpClient, problemRepo, opts, logger);
        });

        // AI
        services.Configure<OpenAiOptions>(options =>
        {
            options.ApiKey = configuration[$"{OpenAiOptions.SectionName}:ApiKey"] ?? string.Empty;
            options.Model = configuration[$"{OpenAiOptions.SectionName}:Model"] ?? OpenAiOptions.DefaultModel;
        });
        services.AddSingleton<ILLMClient, OpenAiChatClient>();
        services.AddSingleton<IPromptLoader, PromptLoader>();

        services.AddScoped<ITutorAgentTools, TutorAgentTools>();
        services.AddScoped<ITutorAgent, TutorAgentService>();

        services.AddScoped<ICodeAnalysisAgentTools, CodeAnalysisAgentTools>();
        services.AddScoped<ICodeAnalysisAgent, CodeAnalysisAgentService>();

        services.AddScoped<IAnalyticsAgentTools, AnalyticsAgentTools>();
        services.AddScoped<IAnalyticsAgent, AnalyticsAgentService>();

        return services;
    }
}
