using MyApp.Domain.Entities;

namespace MyApp.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    (string token, DateTime expiresAt) GenerateRefreshToken();
}
