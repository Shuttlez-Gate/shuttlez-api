using Shuttlez.Application.Admin.Services;
using Shuttlez.Application.Bookings;
using Shuttlez.Application.Pricing;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Admin.DTOs;

public record VehicleCapacityInfoDto(
    string VehicleType,
    string DisplayName,
    int Capacity,
    string Source);

public record RouteLaunchPlanDto(
    string RouteKey,
    Guid? RouteId,
    string RouteLabel,
    string From,
    string To,
    int Demand,
    int UniquePassengers,
    int Confirmed,
    int? RemainingDemand,
    string? VehicleType,
    string? VehicleDisplayName,
    int? Capacity,
    string? CapacitySource,
    bool PricingAvailable,
    bool PricingLinked,
    Guid? PricingRuleId,
    string? PricingSource,
    decimal? OneWayPrice,
    decimal? RoundTripPrice,
    decimal? WeeklyPrice,
    decimal? MonthlyPrice,
    /// <summary>Planning display status (Phase 3 labels mapped from Phase 2 readiness).</summary>
    string LaunchStatus,
    string ReasonCode,
    string ReadinessReason,
    int? MinimumLaunchRiders,
    int? TargetOccupancy,
    int? RequiredRiders,
    int? RemainingRiders,
    int? CurrentOccupancy,
    decimal? OccupancyPercentage,
    int? ExpectedSeats,
    decimal? PricePerSeat,
    decimal? EstimatedGrossRevenue,
    decimal? EstimatedPlatformCommission,
    decimal? EstimatedCaptainEarnings,
    decimal? CommissionRate,
    string? CommissionType,
    bool IsEstimate);

public record RouteLaunchPlanSummaryDto(
    int TotalRoutes,
    int CollectingDemand,
    int AlmostReady,
    int ReadyToLaunch,
    int Full,
    int PricingMissing,
    int VehicleConfigMissing,
    int DataIncomplete);

public record RouteLaunchPlanResponseDto(
    IReadOnlyList<RouteLaunchPlanDto> Items,
    RouteLaunchPlanSummaryDto Summary);

/// <summary>
/// Maps Phase 2 readiness → Phase 3 launch-planning view.
/// Planning estimates use targetOccupancy as expectedSeats (not inventing capacity fill).
/// </summary>
public static class RouteLaunchPlanMapper
{
    public static string ToPlanningStatus(
        DemandLaunchReadinessStatus status,
        string reasonCode,
        int demand,
        int? capacity)
    {
        if (reasonCode == DemandLaunchReasonCodes.AmbiguousRouteMatch)
            return "DATA_INCOMPLETE";

        if (capacity is > 0 && demand > capacity.Value &&
            status is DemandLaunchReadinessStatus.Ready or DemandLaunchReadinessStatus.AlmostReady)
        {
            return "FULL";
        }

        return status switch
        {
            DemandLaunchReadinessStatus.Ready => "READY_TO_LAUNCH",
            DemandLaunchReadinessStatus.AlmostReady => "ALMOST_READY",
            DemandLaunchReadinessStatus.NotReady => "COLLECTING_DEMAND",
            DemandLaunchReadinessStatus.NoPricing => "PRICING_NOT_CONFIGURED",
            DemandLaunchReadinessStatus.MissingConfiguration => "DATA_INCOMPLETE",
            DemandLaunchReadinessStatus.Unknown when reasonCode is
                DemandLaunchReasonCodes.MissingVehicle or DemandLaunchReasonCodes.MissingCapacity
                => "NO_VEHICLE_CONFIG",
            DemandLaunchReadinessStatus.Unknown => "DATA_INCOMPLETE",
            _ => "DATA_INCOMPLETE"
        };
    }

    public static int PlanningSortRank(string planningStatus) => planningStatus switch
    {
        "READY_TO_LAUNCH" => 1,
        "ALMOST_READY" => 2,
        "FULL" => 3,
        "COLLECTING_DEMAND" => 4,
        "PRICING_NOT_CONFIGURED" => 5,
        "DATA_INCOMPLETE" => 6,
        "NO_VEHICLE_CONFIG" => 7,
        _ => 99
    };

    public static RouteLaunchPlanDto FromReadiness(
        RouteDemandReadinessDto readiness,
        string routeKey,
        string routeLabel,
        string from,
        string to,
        string? capacitySource)
    {
        var status = Enum.TryParse<DemandLaunchReadinessStatus>(
            MapApiToEnum(readiness.LaunchStatus), true, out var parsed)
            ? parsed
            : DemandLaunchReadinessStatus.Unknown;

        // Prefer reason-based reconstruction from API status string
        status = readiness.LaunchStatus switch
        {
            "READY" => DemandLaunchReadinessStatus.Ready,
            "ALMOST_READY" => DemandLaunchReadinessStatus.AlmostReady,
            "NOT_READY" => DemandLaunchReadinessStatus.NotReady,
            "NO_PRICING" => DemandLaunchReadinessStatus.NoPricing,
            "MISSING_CONFIGURATION" => DemandLaunchReadinessStatus.MissingConfiguration,
            "UNKNOWN" => DemandLaunchReadinessStatus.Unknown,
            _ => status
        };

        var planningStatus = ToPlanningStatus(
            status,
            readiness.ReasonCode,
            readiness.DemandCount,
            readiness.Capacity);

        var reasonCode = readiness.ReasonCode;
        var reason = readiness.ReadinessReason;
        if (readiness.Capacity is > 0 && readiness.DemandCount > readiness.Capacity.Value)
        {
            reasonCode = DemandLaunchReasonCodes.DemandExceedsCapacity;
            reason = $"الطلب ({readiness.DemandCount}) يتجاوز السعة ({readiness.Capacity}). " + reason;
        }

        int? expectedSeats = null;
        decimal? gross = null;
        decimal? platform = null;
        decimal? captain = null;
        decimal? pricePerSeat = null;
        var isEstimate = false;

        // Zero demand → no financial estimate (planning safety).
        if (readiness.DemandCount > 0 &&
            readiness.PricingAvailable &&
            readiness.OneWayPrice is decimal ow &&
            readiness.TargetOccupancy is int target &&
            target > 0 &&
            readiness.CommissionRate is decimal commissionPct)
        {
            expectedSeats = target;
            pricePerSeat = ow;
            gross = ShuttlePricingCalculator.CalculateGross(ow, target);
            (_, platform, captain) = ShuttleFinancialCalculator.SplitEarnings(gross.Value, commissionPct);
            isEstimate = true;
        }

        int? remainingDemand = readiness.Capacity is int cap
            ? Math.Max(cap - readiness.DemandCount, 0)
            : null;

        return new RouteLaunchPlanDto(
            routeKey,
            readiness.RouteId,
            routeLabel,
            from,
            to,
            readiness.DemandCount,
            readiness.UniquePassengers,
            readiness.ConfirmedPassengers,
            remainingDemand,
            readiness.VehicleType,
            readiness.VehicleTypeName,
            readiness.Capacity,
            capacitySource,
            readiness.PricingAvailable,
            readiness.PricingLinked,
            readiness.PricingRuleId,
            readiness.PricingSource,
            readiness.OneWayPrice,
            readiness.RoundTripPrice,
            readiness.WeeklyPrice,
            readiness.MonthlyPrice,
            planningStatus,
            reasonCode,
            reason,
            readiness.MinimumLaunchRiders,
            readiness.TargetOccupancy,
            readiness.MinimumLaunchRiders,
            readiness.RidersRequired,
            readiness.ConfirmedPassengers,
            readiness.OccupancyPercent,
            expectedSeats,
            pricePerSeat,
            gross,
            platform,
            captain,
            readiness.CommissionRate,
            readiness.CommissionType,
            isEstimate);
    }

    private static string MapApiToEnum(string api) => api switch
    {
        "READY" => nameof(DemandLaunchReadinessStatus.Ready),
        "ALMOST_READY" => nameof(DemandLaunchReadinessStatus.AlmostReady),
        "NOT_READY" => nameof(DemandLaunchReadinessStatus.NotReady),
        "NO_PRICING" => nameof(DemandLaunchReadinessStatus.NoPricing),
        "MISSING_CONFIGURATION" => nameof(DemandLaunchReadinessStatus.MissingConfiguration),
        _ => nameof(DemandLaunchReadinessStatus.Unknown)
    };

    public static RouteLaunchPlanSummaryDto BuildSummary(IReadOnlyList<RouteLaunchPlanDto> items) =>
        new(
            items.Count,
            items.Count(i => i.LaunchStatus == "COLLECTING_DEMAND"),
            items.Count(i => i.LaunchStatus == "ALMOST_READY"),
            items.Count(i => i.LaunchStatus == "READY_TO_LAUNCH"),
            items.Count(i => i.LaunchStatus == "FULL"),
            items.Count(i => i.LaunchStatus == "PRICING_NOT_CONFIGURED"),
            items.Count(i => i.LaunchStatus == "NO_VEHICLE_CONFIG"),
            items.Count(i => i.LaunchStatus == "DATA_INCOMPLETE"));

    public static string VehicleDisplayName(VehicleType type) => type switch
    {
        VehicleType.CarShuttle => "Shuttlez Car",
        VehicleType.MiniBus => "Microbus",
        VehicleType.Bus => "Bus",
        _ => type.ToString()
    };
}
