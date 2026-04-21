using Codify.Domain.Entities;

namespace Codify.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
    DateTime GetExpiry();
}
