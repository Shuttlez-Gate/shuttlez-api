using Shuttlez.Domain.Entities;

namespace Shuttlez.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Guid? GetUserIdFromExpiredToken(string token);
}
