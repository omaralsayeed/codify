using Codify.Domain.Entities;
using Codify.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Codify.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds a default admin account if no admin user exists in the database.
/// Runs automatically on startup via Program.cs.
///
/// Default credentials (development only — change before any shared deployment):
///   Email:    admin@codify.com
///   Password: Admin@123456
///
/// After first login, go to /admin/overview.
/// The JWT token from POST /api/auth/login can be used directly in Swagger.
/// </summary>
public static class AdminSeed
{
    private const string DefaultEmail    = "admin@codify.com";
    private const string DefaultPassword = "Admin@123456";
    private const string DefaultFullName = "Codify Admin";

    public static async Task SeedAsync(CodifyDbContext db)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword);

        var existing = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == DefaultEmail);
        if (existing is not null)
        {
            existing.ResetCredentials(passwordHash, UserRole.Admin, UserStatus.Active);
            await db.SaveChangesAsync();
            return;
        }

        var admin = User.Create(
            fullName:     DefaultFullName,
            email:        DefaultEmail,
            passwordHash: passwordHash,
            role:         UserRole.Admin);

        await db.Users.AddAsync(admin);
        await db.SaveChangesAsync();
    }
}
