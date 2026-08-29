namespace Shuttlez.Application.Pricing;

/// Catalog trip type for Admin preview only — not a booking product kind.
public enum PricingTripType
{
    OneWay = 1,
    RoundTrip = 2
}

public enum RouteLaunchStatus
{
    CollectingDemand,
    AlmostReady,
    ReadyToLaunch,
    Full
}

/// Pure pricing / launch / commission period math (decimal only).
public static class ShuttlePricingCalculator
{
    public const int MoneyDecimals = 2;

    public static decimal NormalizeMoney(decimal value) =>
        Math.Round(value, MoneyDecimals, MidpointRounding.AwayFromZero);

    public static decimal ResolveUnitPrice(
        decimal oneWayPrice,
        decimal roundTripPrice,
        PricingTripType tripType) =>
        tripType == PricingTripType.RoundTrip
            ? NormalizeMoney(roundTripPrice)
            : NormalizeMoney(oneWayPrice);

    public static decimal CalculateGross(decimal unitPrice, int passengerCount)
    {
        if (passengerCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(passengerCount));
        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice));
        return NormalizeMoney(unitPrice * passengerCount);
    }

    /// <summary>
    /// Platform commission percent for as-of date relative to launch window.
    /// </summary>
    public static decimal ResolveCommissionPercent(
        decimal launchCommissionPercent,
        decimal permanentCommissionPercent,
        int launchPeriodDays,
        DateTime launchStartUtc,
        DateTime asOfUtc)
    {
        var launchPct = Math.Clamp(launchCommissionPercent, 0m, 100m);
        var permanentPct = Math.Clamp(permanentCommissionPercent, 0m, 100m);
        if (launchPeriodDays <= 0)
            return permanentPct;

        var launchEnd = launchStartUtc.AddDays(launchPeriodDays);
        return asOfUtc < launchEnd ? launchPct : permanentPct;
    }

    public static DateTime ResolveLaunchStart(
        DateTime? launchStartAt,
        DateTime? effectiveFrom,
        DateTime createdAt) =>
        launchStartAt ?? effectiveFrom ?? createdAt;

    public static RouteLaunchStatus ResolveLaunchStatus(
        int riders,
        int minimumLaunchRiders,
        int targetOccupancy,
        int capacity)
    {
        var min = Math.Max(1, minimumLaunchRiders);
        var target = Math.Max(min, targetOccupancy);
        var cap = Math.Max(1, capacity);
        var safeRiders = Math.Max(0, riders);

        if (safeRiders >= cap)
            return RouteLaunchStatus.Full;
        if (safeRiders >= target)
            return RouteLaunchStatus.ReadyToLaunch;
        if (safeRiders >= min)
            return RouteLaunchStatus.AlmostReady;
        return RouteLaunchStatus.CollectingDemand;
    }

    public static decimal OccupancyPercent(int riders, int capacity)
    {
        if (capacity <= 0) return 0m;
        return NormalizeMoney(100m * riders / capacity);
    }
}
