using System.Collections.Concurrent;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace Shuttlez.API.Middleware;

/// <summary>
/// يحمي GET /api/v1/drivers/me/trips من الـ polling العدواني (حتى لو التطبيق القديم ما زال يعمل polling).
/// حد: 12 طلب / دقيقة لكل مستخدم أو IP.
/// </summary>
public sealed class DriverTripsRateLimitMiddleware
{
    private static readonly ConcurrentDictionary<string, WindowCounter> Counters = new();
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    private const int MaxRequestsPerWindow = 12;

    private readonly RequestDelegate _next;

    public DriverTripsRateLimitMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsTripsGet(context))
        {
            await _next(context);
            return;
        }

        var key = ResolveKey(context);
        var counter = Counters.GetOrAdd(key, _ => new WindowCounter());
        if (!counter.TryIncrement(Window, MaxRequestsPerWindow))
        {
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers.RetryAfter = "60";
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                success = false,
                message = "تم تجاوز حد الطلبات. استخدم SignalR بدل polling.",
                errors = Array.Empty<string>()
            }));
            return;
        }

        await _next(context);
    }

    private static bool IsTripsGet(HttpContext context) =>
        HttpMethods.IsGet(context.Request.Method)
        && context.Request.Path.StartsWithSegments("/api/v1/drivers/me/trips");

    private static string ResolveKey(HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");
        if (!string.IsNullOrWhiteSpace(userId))
        {
            return $"u:{userId}";
        }

        return $"ip:{context.Connection.RemoteIpAddress}";
    }

    private sealed class WindowCounter
    {
        private readonly object _gate = new();
        private DateTime _windowStart = DateTime.UtcNow;
        private int _count;

        public bool TryIncrement(TimeSpan window, int max)
        {
            lock (_gate)
            {
                var now = DateTime.UtcNow;
                if (now - _windowStart >= window)
                {
                    _windowStart = now;
                    _count = 0;
                }

                if (_count >= max)
                {
                    return false;
                }

                _count++;
                return true;
            }
        }
    }
}
