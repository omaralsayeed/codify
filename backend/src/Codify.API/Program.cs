using System.Text;
using System.Threading.RateLimiting;
using Codify.API.Common;
using Codify.API.Middleware;
using Codify.Application.Interfaces;
using Codify.Infrastructure;
using Codify.Infrastructure.Persistence;
using Codify.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;


var builder = WebApplication.CreateBuilder(args);

// Infrastructure (DB, repos, services)
builder.Services.AddInfrastructure(builder.Configuration);

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

// Rate limiting — per-user sliding window policies
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // POST /submissions — 30 per hour per user
    options.AddPolicy("submissions", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                          ?? httpContext.Connection.RemoteIpAddress?.ToString()
                          ?? "anonymous",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromHours(1),
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // POST /execution/run — 60 per hour per user
    options.AddPolicy("execution", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                          ?? httpContext.Connection.RemoteIpAddress?.ToString()
                          ?? "anonymous",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromHours(1),
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // POST /api/hints — 10 per hour per user
    options.AddPolicy("ai-hints", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                          ?? httpContext.Connection.RemoteIpAddress?.ToString()
                          ?? "anonymous",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromHours(1),
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // GET /api/analytics/* — 60 per hour per user
    // Analytics endpoints run heavier DB queries, so a lighter cap than execution.
    options.AddPolicy("analytics", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                          ?? httpContext.Connection.RemoteIpAddress?.ToString()
                          ?? "anonymous",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromHours(1),
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // POST /api/ai/tagging/* — 5 per hour per user
    // Each tagging call costs an LLM round-trip (and the scan costs one per problem).
    options.AddPolicy("ai-tagging", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                          ?? httpContext.Connection.RemoteIpAddress?.ToString()
                          ?? "anonymous",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromHours(1),
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

builder.Services.AddAuthorization();
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var firstError = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .SelectMany(e => e.Value!.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Invalid request.";

            var response = ApiResponse.Fail("VALIDATION_ERROR", firstError);
            return new BadRequestObjectResult(response);
        };
    })
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Microsoft.OpenApi 2.x (Swashbuckle 10): types are in Microsoft.OpenApi,
    // NOT Microsoft.OpenApi.Models — the sub-namespace no longer exists.
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title   = "Codify API",
        Version = "v1"
    });

    // Adds the "Authorize" button to Swagger UI for JWT Bearer tokens
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = Microsoft.OpenApi.SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = Microsoft.OpenApi.ParameterLocation.Header,
        Description  = "Enter your JWT token. It will be sent as: Authorization: Bearer <token>"
    });

    options.AddSecurityRequirement(_ => new Microsoft.OpenApi.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});

// CORS for Angular dev server
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();

// Run migrations and seed on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CodifyDbContext>();
    db.Database.Migrate();
    await ConceptTagSeed.SeedAsync(db);
    await ProblemSeed.SeedAsync(db);
    await AdminSeed.SeedAsync(db);
}

// Auto-ingest concept tags into Chroma Cloud on startup (fire-and-forget).
// Only runs when an OpenAI key is configured. Failures are logged but never
// block application startup — the Tutor Agent degrades gracefully without RAG.
var openAiKey = app.Configuration["OpenAI:ApiKey"];
if (!string.IsNullOrWhiteSpace(openAiKey))
{
    _ = Task.Run(async () =>
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var ingestion = scope.ServiceProvider.GetRequiredService<IKnowledgeBaseIngestionService>();
            var count = await ingestion.IngestAllConceptsAsync();
            app.Logger.LogInformation(
                "Startup RAG ingestion complete: {Count} concept documents indexed.", count);
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex,
                "Startup RAG ingestion failed. The Tutor Agent will run without RAG context until reindex is triggered.");
        }
    });
}

// Enable Swagger UI. For local debugging it's useful to expose Swagger even when the
// environment or tooling might not mark the process as Development. This is safe
// for local/dev only; ensure you do NOT deploy Swagger UI to production in real
// deployments. If you need stricter control, revert to the env-gated block above.
// Enable Swagger in non-production environments (Development, Staging). Keep Swagger
// disabled in Production to avoid exposing API documentation publicly.

// http://localhost:5237/swagger


if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Codify API v1");
        // Serve Swagger UI at app root for convenience in non-prod environments
        c.RoutePrefix = "swagger";
    });
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
