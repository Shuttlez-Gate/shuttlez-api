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

    public Guid? UserId
    {
        get
        {
            var id = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(id, out var userId) ? userId : null;
        }
    }

    public string? Phone =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.MobilePhone)
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("phone_number");

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
}
