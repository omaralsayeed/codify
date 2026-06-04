using System.Text;
using Codify.API.Middleware;
using Codify.Infrastructure;
using Codify.Infrastructure.Persistence;
using Codify.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
    db.Database.EnsureCreated();
    await ConceptTagSeed.SeedAsync(db);
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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
