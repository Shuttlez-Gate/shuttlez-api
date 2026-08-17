using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Filters;
using Shuttlez.Application.Common;

namespace Shuttlez.API.Filters;

/// <summary>
/// يفرض دور Admin على الأكشن. نستخدم فلتر بدلاً من <c>[Authorize]</c> لأن المشروع
/// يسجّل <c>AllowAnonymousFilter</c> عالمياً لخدمة تطبيق الموبايل، فلا تعمل
/// سياسات التخويل القياسية.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AdminOnlyAttribute : Attribute, IAsyncActionFilter
{
    public const string RoleName = "Admin";

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAppException("يجب تسجيل الدخول كمسؤول");
        }

        if (!IsAdmin(user))
        {
            throw new ForbiddenAppException("هذا الإجراء متاح للمسؤولين فقط");
        }

        await next();
    }

    private static bool IsAdmin(ClaimsPrincipal user)
    {
        if (user.IsInRole(RoleName))
        {
            return true;
        }

        return user.Claims.Any(c =>
            IsRoleClaim(c.Type)
            && string.Equals(c.Value, RoleName, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsRoleClaim(string type) =>
        type is "role" or "roles"
        || type == ClaimTypes.Role
        || type.EndsWith("/identity/claims/role", StringComparison.OrdinalIgnoreCase);
}
