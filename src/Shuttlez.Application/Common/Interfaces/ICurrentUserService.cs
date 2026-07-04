namespace Shuttlez.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Phone { get; }
    bool IsAuthenticated { get; }
}
