using System.Text;
using System.Threading.RateLimiting;
using Codify.API.Middleware;
using Codify.Application.Interfaces;
using Codify.Infrastructure;
using Codify.Infrastructure.Persistence;
using Codify.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
}

// Automatic Tagging Agent scan: tag all currently-untagged problems on startup.
// Fire-and-forget so it never blocks boot; only runs when the feature is enabled
// AND an OpenAI key is configured (otherwise every classification would fail).
var autoTagOnStartup   = builder.Configuration.GetValue("Tagging:AutoTagUntaggedOnStartup", false);
var openAiKeyPresent   = !string.IsNullOrWhiteSpace(builder.Configuration["OpenAI:ApiKey"]);
if (autoTagOnStartup && openAiKeyPresent)
{
    _ = Task.Run(async () =>
    {
        using var scanScope = app.Services.CreateScope();
        var scanLogger = scanScope.ServiceProvider
            .GetRequiredService<ILoggerFactory>().CreateLogger("TaggingAutoScan");
        try
        {
            var taggingService = scanScope.ServiceProvider.GetRequiredService<ITaggingService>();
            var scan = await taggingService.TagAllUntaggedProblemsAsync();
            scanLogger.LogInformation(
                "Startup tagging scan tagged {Tagged}/{Found} untagged problems.",
                scan.Tagged, scan.UntaggedFound);
        }
        catch (Exception ex)
        {
            scanLogger.LogError(ex, "Startup tagging scan failed.");
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
