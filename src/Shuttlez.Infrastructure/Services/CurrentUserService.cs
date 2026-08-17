using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Shuttlez.Application.Common.Interfaces;

namespace Shuttlez.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var id = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User?.FindFirstValue("sub")
                ?? User?.FindFirstValue("nameid");
            return Guid.TryParse(id, out var userId) ? userId : null;
        }
    }

    public string? Phone =>
        User?.FindFirstValue(ClaimTypes.MobilePhone)
        ?? User?.FindFirstValue("phone_number");

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public string? Role =>
        User?.FindFirstValue(ClaimTypes.Role) ?? User?.FindFirstValue("role");

    public bool IsAdmin =>
        string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase);
}
