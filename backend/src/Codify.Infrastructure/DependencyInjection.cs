using Codify.Application.Agents;
using Codify.Application.Interfaces;
using Codify.Application.Services;
using Codify.Infrastructure.AI;
using Codify.Infrastructure.Auth;
using Codify.Infrastructure.BackgroundJobs;
using Codify.Infrastructure.Judge0;
using Codify.Infrastructure.Persistence;
using Codify.Infrastructure.Repositories;
using Codify.Infrastructure.Search;
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
        services.AddScoped<IContestRepository, ContestRepository>();

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
        services.AddScoped<IContestService, ContestService>();
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
            options.ApiKey                    = configuration[$"{OpenAiOptions.SectionName}:ApiKey"] ?? string.Empty;
            options.Model                     = configuration[$"{OpenAiOptions.SectionName}:Model"] ?? OpenAiOptions.DefaultModel;
            options.EscalationModel           = configuration[$"{OpenAiOptions.SectionName}:EscalationModel"] ?? OpenAiOptions.DefaultEscalationModel;
            options.EmbeddingModel            = configuration[$"{OpenAiOptions.SectionName}:EmbeddingModel"] ?? OpenAiOptions.DefaultEmbeddingModel;
            options.BaseUrl                   = configuration[$"{OpenAiOptions.SectionName}:BaseUrl"] ?? string.Empty;
            if (int.TryParse(configuration[$"{OpenAiOptions.SectionName}:EscalationAttemptThreshold"], out var attemptThreshold))
                options.EscalationAttemptThreshold = attemptThreshold;
            if (int.TryParse(configuration[$"{OpenAiOptions.SectionName}:EscalationHintLevelThreshold"], out var hintThreshold))
                options.EscalationHintLevelThreshold = hintThreshold;
        });

        // Chroma Cloud (vector database for RAG)
        services.Configure<ChromaCloudOptions>(configuration.GetSection(ChromaCloudOptions.SectionName));

        services.AddSingleton<ILLMClient, OpenAiChatClient>();
        services.AddSingleton<IPromptLoader, PromptLoader>();

        // RAG layer: embeddings -> Chroma Cloud vector store -> knowledge base search
        services.AddHttpClient<IEmbeddingService, OpenAiEmbeddingService>((sp, client) =>
        {
            var openAi = sp.GetRequiredService<IOptions<OpenAiOptions>>().Value;
            var baseUrl = string.IsNullOrWhiteSpace(openAi.BaseUrl)
                ? "https://api.openai.com/v1/"
                : openAi.BaseUrl.TrimEnd('/') + "/";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
            if (!string.IsNullOrWhiteSpace(openAi.ApiKey))
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", openAi.ApiKey);
        });

        services.AddHttpClient<IVectorStore, ChromaCloudVectorStore>((sp, client) =>
        {
            var chroma = sp.GetRequiredService<IOptions<ChromaCloudOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(chroma.Endpoint))
                client.BaseAddress = new Uri(chroma.Endpoint.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(chroma.TimeoutSeconds > 0 ? chroma.TimeoutSeconds : 20);
            if (!string.IsNullOrWhiteSpace(chroma.ApiKey))
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", chroma.ApiKey);
        });

        services.AddScoped<IKnowledgeBaseSearchService, KnowledgeBaseSearchService>();
        services.AddScoped<IKnowledgeBaseIngestionService, KnowledgeBaseIngestionService>();

        // Tutor Agent (agentic tool-calling)
        services.AddScoped<ITutorAgentTools, TutorAgentTools>();
        services.AddScoped<ITutorAgent, TutorAgentService>();

        // Code Analysis Agent (static workflow fired after evaluation)
        services.AddScoped<ICodeCheckerAgent, CodeAnalysisAgentService>();

        // Tagging Agent (static workflow: tags problems + refreshes user tags on progress)
        services.AddScoped<ITaggingAgent, TaggingAgentService>();
        services.AddScoped<ITaggingService, TaggingService>();

        return services;
    }
}
