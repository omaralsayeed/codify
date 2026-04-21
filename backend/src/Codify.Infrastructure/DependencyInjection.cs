using Codify.Application.Interfaces;
using Codify.Application.Services;
using Codify.Infrastructure.Auth;
using Codify.Infrastructure.Persistence;
using Codify.Infrastructure.Repositories;
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

        // Auth
        services.AddScoped<IJwtService, JwtService>();

        // Application Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProblemService, ProblemService>();
        services.AddScoped<IConceptTagService, ConceptTagService>();
        services.AddScoped<ISubmissionService, SubmissionService>();

        return services;
    }
}
