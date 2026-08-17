namespace Shuttlez.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Phone { get; }
    bool IsAuthenticated { get; }

    /// <summary>اسم الدور كما في claim الـ JWT: Passenger / Driver / Admin.</summary>
    string? Role { get; }

    bool IsAdmin { get; }
}
