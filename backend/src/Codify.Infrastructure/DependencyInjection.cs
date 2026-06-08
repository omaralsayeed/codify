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
        services.AddDbContext(options =>
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

        // Quick Run
        services.AddScoped<IQuickRunService, QuickRunService>();
<<<<<<< HEAD

        // Execution - Judge0 sandboxed execution
        services.Configure<Judge0Options>(options =>
        {
            options.BaseUrl =
                configuration[$"{Judge0Options.SectionName}:BaseUrl"]
                ?? "http://localhost:2358";

            options.AuthToken =
                configuration[$"{Judge0Options.SectionName}:AuthToken"]
                ?? string.Empty;

            options.TimeoutMs =
                int.TryParse(
                    configuration[$"{Judge0Options.SectionName}:TimeoutMs"],
                    out var timeout)
                    ? timeout
                    : 30000;
        });

        services.AddHttpClient<Judge0ExecutionService>((sp, client) =>
        {
            var opts = sp
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<Judge0Options>>()
                .Value;

            client.BaseAddress = new Uri(opts.BaseUrl);
            client.Timeout = TimeSpan.FromMilliseconds(opts.TimeoutMs);
        });

        services.AddScoped<IExecutionService>(sp =>
        {
            var httpClient =
                sp.GetRequiredService<IHttpClientFactory>()
                    .CreateClient(nameof(Judge0ExecutionService));

            var problemRepo =
                sp.GetRequiredService<IProblemRepository>();

            var opts =
                sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Judge0Options>>();

            var logger =
                sp.GetRequiredService<
                    Microsoft.Extensions.Logging.ILogger<Judge0ExecutionService>>();

            return new Judge0ExecutionService(
                httpClient,
                problemRepo,
                opts,
                logger);
        });
=======
        services.AddScoped<IQuickRunWithTestsService, QuickRunWithTestsService>();
        services.AddScoped<IAiHintService, AiHintService>();
>>>>>>> 6638ed5 (feat(judge): add test case runner, output validation, timeout and memory protection)

        // AI
        services.Configure<OpenAiOptions>(options =>
        {
            options.ApiKey =
                configuration[$"{OpenAiOptions.SectionName}:ApiKey"]
                ?? string.Empty;

            options.Model =
                configuration[$"{OpenAiOptions.SectionName}:Model"]
                ?? OpenAiOptions.DefaultModel;

            options.EmbeddingModel =
                configuration[$"{OpenAiOptions.SectionName}:EmbeddingModel"]
                ?? "text-embedding-3-small";
        });

        services.AddSingleton<ILLMClient, OpenAiChatClient>();
        services.AddSingleton<IPromptLoader, PromptLoader>();

        // RAG - embeddings + vector store (Chroma)
        services.Configure<ChromaOptions>(options =>
        {
            options.BaseUrl =
                configuration[$"{ChromaOptions.SectionName}:BaseUrl"]
                ?? "http://localhost:8000";

            options.CollectionName =
                configuration[$"{ChromaOptions.SectionName}:CollectionName"]
                ?? "codify_knowledge";

            options.TimeoutMs =
                int.TryParse(
                    configuration[$"{ChromaOptions.SectionName}:TimeoutMs"],
                    out var timeout)
                    ? timeout
                    : 30000;

            options.SimilarityThreshold =
                float.TryParse(
                    configuration[$"{ChromaOptions.SectionName}:SimilarityThreshold"],
                    out var similarity)
                    ? similarity
                    : 0.75f;
        });

        services.AddHttpClient<OpenAiEmbeddingService>((sp, client) =>
        {
            var opts =
                sp.GetRequiredService<
                    Microsoft.Extensions.Options.IOptions<OpenAiOptions>>()
                    .Value;

            client.BaseAddress = new Uri("https://api.openai.com/v1/");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    opts.ApiKey);

            client.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddScoped<IEmbeddingService>(sp =>
        {
            var httpClient =
                sp.GetRequiredService<IHttpClientFactory>()
                    .CreateClient(nameof(OpenAiEmbeddingService));

            var opts =
                sp.GetRequiredService<
                    Microsoft.Extensions.Options.IOptions<OpenAiOptions>>();

            var logger =
                sp.GetRequiredService<
                    Microsoft.Extensions.Logging.ILogger<OpenAiEmbeddingService>>();

            return new OpenAiEmbeddingService(
                httpClient,
                opts,
                logger);
        });

        services.AddHttpClient<ChromaVectorStore>((sp, client) =>
        {
            var opts =
                sp.GetRequiredService<
                    Microsoft.Extensions.Options.IOptions<ChromaOptions>>()
                    .Value;

            client.BaseAddress = new Uri(opts.BaseUrl);
            client.Timeout = TimeSpan.FromMilliseconds(opts.TimeoutMs);
        });

        services.AddScoped<IVectorStore>(sp =>
        {
            var httpClient =
                sp.GetRequiredService<IHttpClientFactory>()
                    .CreateClient(nameof(ChromaVectorStore));

            var opts =
                sp.GetRequiredService<
                    Microsoft.Extensions.Options.IOptions<ChromaOptions>>();

            var logger =
                sp.GetRequiredService<
                    Microsoft.Extensions.Logging.ILogger<ChromaVectorStore>>();

            return new ChromaVectorStore(
                httpClient,
                opts,
                logger);
        });

        services.AddScoped<IConceptDocumentIngestionService, ConceptDocumentIngestionService>();

        // Tutor Agent
        services.AddScoped<ITutorAgentTools, TutorAgentTools>();
        services.AddScoped<ITutorAgent, TutorAgentService>();

        // Code Analysis Agent
        services.AddScoped<ICodeAnalysisAgentTools, CodeAnalysisAgentTools>();
        services.AddScoped<ICodeAnalysisAgent, CodeAnalysisAgentService>();

        // Analytics Agent
        services.AddScoped<IAnalyticsAgentTools, AnalyticsAgentTools>();
        services.AddScoped<IAnalyticsAgent, AnalyticsAgentService>();

        return services;
    }
}
