using Shuttlez.Application.Bookings;
using Shuttlez.Application.Pricing;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Admin.Services;

/// <summary>
/// Strict Admin launch-readiness statuses (decision support only — not operational).
/// </summary>
public enum DemandLaunchReadinessStatus
{
    Ready,
    AlmostReady,
    NotReady,
    NoPricing,
    MissingConfiguration,
    Unknown
}

public static class DemandLaunchReasonCodes
{
    public const string MissingRouteLink = "MISSING_ROUTE_LINK";
    public const string NoPricing = "NO_PRICING";
    public const string MissingConfiguration = "MISSING_CONFIGURATION";
    public const string MissingCapacity = "MISSING_CAPACITY";
    public const string MissingVehicle = "MISSING_VEHICLE";
    public const string InvalidLaunchConfig = "INVALID_LAUNCH_CONFIG";
    public const string BelowMinimum = "BELOW_MINIMUM";
    public const string BetweenMinAndTarget = "BETWEEN_MIN_AND_TARGET";
    public const string MeetsTarget = "MEETS_TARGET";
    public const string AmbiguousRouteMatch = "AMBIGUOUS_ROUTE_MATCH";
    public const string DemandExceedsCapacity = "DEMAND_EXCEEDS_CAPACITY";
    public const string ZeroDemand = "ZERO_DEMAND";
}

public sealed record RouteDemandReadinessInput(
    int DemandCount,
    int ConfirmedPassengers,
    int UniquePassengers,
    Guid? RouteId,
    bool RouteMatchSafe,
    VehicleType? VehicleType,
    int? Capacity,
    Guid? PricingRuleId,
    string? PricingSource,
    decimal? OneWayPrice,
    decimal? RoundTripPrice,
    decimal? WeeklyPrice,
    decimal? MonthlyPrice,
    int? MinimumLaunchRiders,
    int? TargetOccupancy,
    decimal? CommissionPercent,
    string? CommissionType,
    int? LaunchPeriodDays,
    DateTime? LaunchStartAt,
    DateTime? LaunchEndAt,
    bool? LaunchActive,
    bool AmbiguousRouteMatch = false);

public sealed record RouteDemandReadinessResult(
    DemandLaunchReadinessStatus LaunchStatus,
    string ReasonCode,
    string ReadinessReason,
    bool PricingAvailable,
    bool PricingLinked,
    Guid? RouteId,
    VehicleType? VehicleType,
    int? Capacity,
    int DemandCount,
    int ConfirmedPassengers,
    int UniquePassengers,
    decimal? OccupancyPercent,
    int? RidersRequired,
    int? RemainingToTarget,
    int? MinimumLaunchRiders,
    int? TargetOccupancy,
    Guid? PricingRuleId,
    string? PricingSource,
    decimal? OneWayPrice,
    decimal? RoundTripPrice,
    decimal? WeeklyPrice,
    decimal? MonthlyPrice,
    decimal? CommissionPercent,
    string? CommissionType,
    int? LaunchPeriodDays,
    DateTime? LaunchStartAt,
    DateTime? LaunchEndAt,
    bool? LaunchActive,
    /// <summary>Preview at configured minimumLaunchRiders only — not inventing demand.</summary>
    decimal? FinancialAtMinimumOneWayGross,
    decimal? FinancialAtMinimumRoundTripGross,
    decimal? FinancialAtMinimumPlatformCommission,
    decimal? FinancialAtMinimumCaptainEarnings,
    /// <summary>EXPLICIT (Admin map) or EXACT_KEY (name match). Null when unlinked.</summary>
    string? RouteLinkSource = null);

/// <summary>
/// Pure readiness math. Confirmed passengers are the launch metric.
/// No RouteId → UNKNOWN. No fuzzy matching. No invented thresholds.
/// </summary>
public static class RouteDemandReadinessCalculator
{
    public static RouteDemandReadinessResult Evaluate(RouteDemandReadinessInput input)
    {
        var confirmed = Math.Max(0, input.ConfirmedPassengers);

        if (input.AmbiguousRouteMatch)
        {
            return Terminal(
                input,
                DemandLaunchReadinessStatus.Unknown,
                DemandLaunchReasonCodes.AmbiguousRouteMatch,
                "تطابق مسار غامض — أكثر من Route يطابق نفس المفتاح. لا يتم اختيار واحد تلقائياً.",
                pricingAvailable: false,
                pricingLinked: false);
        }

        // 1) Safe route link required — never use vehicle-default pricing as if route-linked.
        if (!input.RouteMatchSafe || input.RouteId is null)
        {
            return Terminal(
                input,
                DemandLaunchReadinessStatus.Unknown,
                DemandLaunchReasonCodes.MissingRouteLink,
                "Route Demand غير مرتبط بـ RouteId رسمي. لا يتم تخمين الربط.",
                pricingAvailable: false,
                pricingLinked: false);
        }

        if (input.VehicleType is null)
        {
            return Terminal(
                input,
                DemandLaunchReadinessStatus.Unknown,
                DemandLaunchReasonCodes.MissingVehicle,
                "نوع المركبة غير متاح بشكل موثوق.",
                pricingAvailable: false,
                pricingLinked: true);
        }

        if (input.Capacity is null || input.Capacity <= 0)
        {
            return Terminal(
                input,
                DemandLaunchReadinessStatus.Unknown,
                DemandLaunchReasonCodes.MissingCapacity,
                "سعة المركبة غير متاحة أو غير صالحة.",
                pricingAvailable: input.PricingRuleId is not null,
                pricingLinked: true);
        }

        if (input.PricingRuleId is null ||
            input.OneWayPrice is null ||
            input.RoundTripPrice is null)
        {
            return Terminal(
                input,
                DemandLaunchReadinessStatus.NoPricing,
                DemandLaunchReasonCodes.NoPricing,
                "لا توجد Pricing Rule فعالة لهذا الخط ونوع المركبة.",
                pricingAvailable: false,
                pricingLinked: true);
        }

        if (input.MinimumLaunchRiders is null || input.TargetOccupancy is null)
        {
            return Terminal(
                input,
                DemandLaunchReadinessStatus.MissingConfiguration,
                DemandLaunchReasonCodes.MissingConfiguration,
                "بيانات الإطلاق ناقصة (الحد الأدنى أو هدف الإشغال).",
                pricingAvailable: true,
                pricingLinked: true);
        }

        var min = input.MinimumLaunchRiders.Value;
        var target = input.TargetOccupancy.Value;
        var capacity = input.Capacity.Value;

        if (min < 1 || target < min || target > capacity)
        {
            return Terminal(
                input,
                DemandLaunchReadinessStatus.MissingConfiguration,
                DemandLaunchReasonCodes.InvalidLaunchConfig,
                "إعداد الإطلاق غير صالح (حد أدنى / هدف / سعة).",
                pricingAvailable: true,
                pricingLinked: true);
        }

        var occupancy = OccupancyFromConfirmed(confirmed, capacity);
        var ridersRequired = Math.Max(min - confirmed, 0);
        var remainingToTarget = Math.Max(target - confirmed, 0);

        DemandLaunchReadinessStatus status;
        string reasonCode;
        string reason;
        if (confirmed < min)
        {
            status = DemandLaunchReadinessStatus.NotReady;
            reasonCode = DemandLaunchReasonCodes.BelowMinimum;
            reason = $"غير جاهز — متبقي {ridersRequired} راكب مؤكّد للوصول إلى الحد الأدنى للإطلاق ({min}).";
        }
        else if (confirmed < target)
        {
            // Explicit targetOccupancy threshold enables ALMOST_READY (not an arbitrary %).
            status = DemandLaunchReadinessStatus.AlmostReady;
            reasonCode = DemandLaunchReasonCodes.BetweenMinAndTarget;
            reason =
                $"قريب من الإطلاق — بلغ الحد الأدنى ({min}) ولم يصل لهدف الإشغال ({target}). متبقي {remainingToTarget}.";
        }
        else
        {
            status = DemandLaunchReadinessStatus.Ready;
            reasonCode = DemandLaunchReasonCodes.MeetsTarget;
            reason = $"جاهز للإطلاق — الركاب المؤكدون ({confirmed}) بلغوا هدف الإشغال ({target}).";
        }

        var (oneWayGross, rtGross, platform, captain) = PreviewAtRiderCount(
            min,
            input.OneWayPrice.Value,
            input.RoundTripPrice.Value,
            input.CommissionPercent ?? 0m);

        return new RouteDemandReadinessResult(
            status,
            reasonCode,
            reason,
            PricingAvailable: true,
            PricingLinked: true,
            input.RouteId,
            input.VehicleType,
            capacity,
            input.DemandCount,
            confirmed,
            input.UniquePassengers,
            occupancy,
            ridersRequired,
            remainingToTarget,
            min,
            target,
            input.PricingRuleId,
            input.PricingSource,
            ShuttlePricingCalculator.NormalizeMoney(input.OneWayPrice.Value),
            ShuttlePricingCalculator.NormalizeMoney(input.RoundTripPrice.Value),
            input.WeeklyPrice is decimal w ? ShuttlePricingCalculator.NormalizeMoney(w) : null,
            input.MonthlyPrice is decimal m ? ShuttlePricingCalculator.NormalizeMoney(m) : null,
            input.CommissionPercent,
            input.CommissionType,
            input.LaunchPeriodDays,
            input.LaunchStartAt,
            input.LaunchEndAt,
            input.LaunchActive,
            oneWayGross,
            rtGross,
            platform,
            captain);
    }

    public static decimal? OccupancyFromConfirmed(int confirmedPassengers, int? capacity)
    {
        if (capacity is null or <= 0) return null;
        return ShuttlePricingCalculator.OccupancyPercent(
            Math.Max(0, confirmedPassengers),
            capacity.Value);
    }

    private static (
        decimal OneWayGross,
        decimal RoundTripGross,
        decimal Platform,
        decimal Captain) PreviewAtRiderCount(
        int riders,
        decimal oneWay,
        decimal roundTrip,
        decimal commissionPercent)
    {
        var ow = ShuttlePricingCalculator.CalculateGross(oneWay, riders);
        var rt = ShuttlePricingCalculator.CalculateGross(roundTrip, riders);
        var (_, platform, captain) = ShuttleFinancialCalculator.SplitEarnings(rt, commissionPercent);
        return (ow, rt, platform, captain);
    }

    private static RouteDemandReadinessResult Terminal(
        RouteDemandReadinessInput input,
        DemandLaunchReadinessStatus status,
        string reasonCode,
        string reason,
        bool pricingAvailable,
        bool pricingLinked)
    {
        decimal? occupancy = OccupancyFromConfirmed(input.ConfirmedPassengers, input.Capacity);
        int? ridersRequired = input.MinimumLaunchRiders is int min
            ? Math.Max(min - Math.Max(0, input.ConfirmedPassengers), 0)
            : null;
        int? remTarget = input.TargetOccupancy is int t
            ? Math.Max(t - Math.Max(0, input.ConfirmedPassengers), 0)
            : null;

        return new RouteDemandReadinessResult(
            status,
            reasonCode,
            reason,
            pricingAvailable,
            pricingLinked,
            input.RouteId,
            input.VehicleType,
            input.Capacity is > 0 ? input.Capacity : null,
            input.DemandCount,
            Math.Max(0, input.ConfirmedPassengers),
            input.UniquePassengers,
            occupancy,
            ridersRequired,
            remTarget,
            input.MinimumLaunchRiders,
            input.TargetOccupancy,
            pricingAvailable ? input.PricingRuleId : null,
            pricingAvailable ? input.PricingSource : null,
            pricingAvailable ? input.OneWayPrice : null,
            pricingAvailable ? input.RoundTripPrice : null,
            pricingAvailable ? input.WeeklyPrice : null,
            pricingAvailable ? input.MonthlyPrice : null,
            pricingAvailable ? input.CommissionPercent : null,
            pricingAvailable ? input.CommissionType : null,
            pricingAvailable ? input.LaunchPeriodDays : null,
            pricingAvailable ? input.LaunchStartAt : null,
            pricingAvailable ? input.LaunchEndAt : null,
            pricingAvailable ? input.LaunchActive : null,
            null,
            null,
            null,
            null);
    }

    public static string ToApiStatus(DemandLaunchReadinessStatus status) => status switch
    {
        DemandLaunchReadinessStatus.Ready => "READY",
        DemandLaunchReadinessStatus.AlmostReady => "ALMOST_READY",
        DemandLaunchReadinessStatus.NotReady => "NOT_READY",
        DemandLaunchReadinessStatus.NoPricing => "NO_PRICING",
        DemandLaunchReadinessStatus.MissingConfiguration => "MISSING_CONFIGURATION",
        DemandLaunchReadinessStatus.Unknown => "UNKNOWN",
        _ => "UNKNOWN"
    };

    public static bool TryParseVehicleType(string? raw, out VehicleType type)
    {
        type = default;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var key = raw.Trim();
        if (Enum.TryParse(key, ignoreCase: true, out type) &&
            Enum.IsDefined(typeof(VehicleType), type))
            return true;

        type = key.ToLowerInvariant().Replace("-", "").Replace("_", "") switch
        {
            "car" or "shuttlecar" or "shuttlezcar" or "carshuttle" or "shuttlez" => VehicleType.CarShuttle,
            "microbus" or "minibus" or "minibuss" => VehicleType.MiniBus,
            "bus" => VehicleType.Bus,
            _ => default
        };
        return type != default;
    }
}
