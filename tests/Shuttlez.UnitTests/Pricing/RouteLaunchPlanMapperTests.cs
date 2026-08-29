using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Admin.Services;
using Shuttlez.Domain.Enums;

namespace Shuttlez.UnitTests.Pricing;

public class RouteLaunchPlanMapperTests
{
    private static RouteDemandReadinessDto Ready(
        int demand = 10,
        int confirmed = 10,
        int? capacity = 13,
        int min = 8,
        int target = 10,
        decimal? oneWay = 75,
        decimal commission = 10,
        string status = "READY",
        string reason = "MEETS_TARGET",
        bool pricing = true) =>
        new(
            Guid.NewGuid(),
            "حلوان - مصر الجديدة",
            "MiniBus",
            "ميكروباص",
            capacity,
            demand,
            confirmed,
            confirmed,
            capacity is > 0
                ? Shuttlez.Application.Pricing.ShuttlePricingCalculator.OccupancyPercent(confirmed, capacity.Value)
                : null,
            min,
            target,
            Math.Max(min - confirmed, 0),
            Math.Max(target - confirmed, 0),
            pricing,
            true,
            "ROUTE_VEHICLE",
            oneWay,
            oneWay is null ? null : oneWay * 2,
            690,
            2850,
            commission,
            "PERMANENT",
            90,
            null,
            null,
            false,
            null,
            null,
            null,
            null,
            status,
            reason,
            "ok",
            Guid.NewGuid(),
            DateTime.UtcNow);

    [Fact]
    public void MapsReady_ToReadyToLaunch()
    {
        var plan = RouteLaunchPlanMapper.FromReadiness(
            Ready(), "k", "حلوان ↔ مصر الجديدة", "حلوان", "مصر الجديدة", "VEHICLE_MASTER");
        Assert.Equal("READY_TO_LAUNCH", plan.LaunchStatus);
        Assert.Equal(10, plan.ExpectedSeats);
        Assert.Equal(750m, plan.EstimatedGrossRevenue);
        Assert.Equal(75m, plan.EstimatedPlatformCommission);
        Assert.Equal(675m, plan.EstimatedCaptainEarnings);
        Assert.True(plan.IsEstimate);
    }

    [Fact]
    public void AlmostReady_Status()
    {
        var plan = RouteLaunchPlanMapper.FromReadiness(
            Ready(demand: 8, confirmed: 8, status: "ALMOST_READY", reason: "BETWEEN_MIN_AND_TARGET"),
            "k", "L", "a", "b", null);
        Assert.Equal("ALMOST_READY", plan.LaunchStatus);
    }

    [Fact]
    public void CollectingDemand_FromNotReady()
    {
        var plan = RouteLaunchPlanMapper.FromReadiness(
            Ready(demand: 5, confirmed: 5, status: "NOT_READY", reason: "BELOW_MINIMUM"),
            "k", "L", "a", "b", null);
        Assert.Equal("COLLECTING_DEMAND", plan.LaunchStatus);
    }

    [Fact]
    public void NoPricing_NullFinancials()
    {
        var plan = RouteLaunchPlanMapper.FromReadiness(
            Ready(pricing: false, oneWay: null, status: "NO_PRICING", reason: "NO_PRICING") with
            {
                PricingAvailable = false,
                OneWayPrice = null,
                RoundTripPrice = null,
                CommissionRate = null
            },
            "k", "L", "a", "b", null);
        Assert.Equal("PRICING_NOT_CONFIGURED", plan.LaunchStatus);
        Assert.Null(plan.EstimatedGrossRevenue);
        Assert.Null(plan.EstimatedCaptainEarnings);
        Assert.False(plan.IsEstimate);
    }

    [Fact]
    public void ZeroDemand_NoFinancialEstimate()
    {
        var plan = RouteLaunchPlanMapper.FromReadiness(
            Ready(demand: 0, confirmed: 0, status: "NOT_READY"),
            "k", "L", "a", "b", null);
        Assert.Null(plan.EstimatedGrossRevenue);
        Assert.False(plan.IsEstimate);
    }

    [Fact]
    public void DemandExceedsCapacity_SetsReason()
    {
        var plan = RouteLaunchPlanMapper.FromReadiness(
            Ready(demand: 20, confirmed: 10, capacity: 13),
            "k", "L", "a", "b", null);
        Assert.Equal(DemandLaunchReasonCodes.DemandExceedsCapacity, plan.ReasonCode);
        Assert.Equal("FULL", plan.LaunchStatus);
    }

    [Fact]
    public void Ambiguous_MapsDataIncomplete()
    {
        Assert.Equal(
            "DATA_INCOMPLETE",
            RouteLaunchPlanMapper.ToPlanningStatus(
                DemandLaunchReadinessStatus.Unknown,
                DemandLaunchReasonCodes.AmbiguousRouteMatch,
                8,
                13));
    }

    [Fact]
    public void NoVehicle_MapsNoVehicleConfig()
    {
        Assert.Equal(
            "NO_VEHICLE_CONFIG",
            RouteLaunchPlanMapper.ToPlanningStatus(
                DemandLaunchReadinessStatus.Unknown,
                DemandLaunchReasonCodes.MissingCapacity,
                3,
                null));
    }

    [Fact]
    public void Summary_CountsFromItems()
    {
        var items = new List<RouteLaunchPlanDto>
        {
            RouteLaunchPlanMapper.FromReadiness(Ready(status: "READY"), "1", "a", "a", "b", null),
            RouteLaunchPlanMapper.FromReadiness(
                Ready(status: "ALMOST_READY", confirmed: 8, demand: 8), "2", "a", "a", "b", null),
            RouteLaunchPlanMapper.FromReadiness(
                Ready(status: "NO_PRICING", pricing: false, oneWay: null) with
                {
                    PricingAvailable = false,
                    OneWayPrice = null,
                    CommissionRate = null
                },
                "3", "a", "a", "b", null),
        };
        var s = RouteLaunchPlanMapper.BuildSummary(items);
        Assert.Equal(3, s.TotalRoutes);
        Assert.Equal(1, s.ReadyToLaunch);
        Assert.Equal(1, s.AlmostReady);
        Assert.Equal(1, s.PricingMissing);
    }

    [Fact]
    public void VehicleCapacities_DisplayNames()
    {
        Assert.Equal("Shuttlez Car", RouteLaunchPlanMapper.VehicleDisplayName(VehicleType.CarShuttle));
        Assert.Equal("Microbus", RouteLaunchPlanMapper.VehicleDisplayName(VehicleType.MiniBus));
    }
}
