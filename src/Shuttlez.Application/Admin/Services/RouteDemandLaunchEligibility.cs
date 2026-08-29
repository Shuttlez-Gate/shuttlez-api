using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Common;

namespace Shuttlez.Application.Admin.Services;

/// <summary>
/// Pure launch gates — backend recalculates readiness; frontend values are never trusted.
/// </summary>
public static class RouteDemandLaunchEligibility
{
    public const string ReadyStatus = "READY";

    public static void EnsureScheduledAtValid(DateTime scheduledAtUtc, DateTime nowUtc)
    {
        if (scheduledAtUtc <= nowUtc)
            throw new AppException("موعد الرحلة يجب أن يكون في المستقبل.", code: ErrorCodes.InvalidServiceDate);
    }

    public static void EnsureReadyToLaunch(RouteDemandDetailsDto? details)
    {
        if (details is null)
            throw new AppException("مجموعة الطلب غير موجودة.", code: ErrorCodes.RouteDemandNotFound);

        var readiness = details.Readiness
            ?? throw new AppException("تعذّر حساب الجاهزية.", code: ErrorCodes.NotReadyToLaunch);

        if (!readiness.PricingLinked || readiness.RouteId is null)
            throw new AppException("الطلب غير مرتبط بخط رسمي.", code: ErrorCodes.RouteNotLinked);

        if (!string.Equals(readiness.LaunchStatus, ReadyStatus, StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException(
                $"الخط غير جاهز للتشغيل. الحالة الحالية: {readiness.LaunchStatus}. {readiness.ReadinessReason}",
                code: ErrorCodes.NotReadyToLaunch);
        }
    }

    public static bool IsLaunchButtonEnabled(string? launchStatus) =>
        string.Equals(launchStatus, ReadyStatus, StringComparison.OrdinalIgnoreCase);
}
