using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Admin.Services;
using Shuttlez.Application.Common;

namespace Shuttlez.UnitTests.Admin;

public class RouteDemandLaunchEligibilityTests
{
    private static RouteDemandDetailsDto Details(
        string launchStatus,
        bool pricingLinked = true,
        Guid? routeId = null)
    {
        Guid? resolvedRouteId = pricingLinked ? routeId ?? Guid.NewGuid() : null;
        return new(
            RouteKey: "a|b",
            RouteLabel: "A ↔ B",
            EndpointA: "A",
            EndpointB: "B",
            TotalRequests: 10,
            ConfirmedPassengers: 8,
            UniquePassengers: 8,
            RecommendedVehicle: "MiniBus",
            VehicleCapacity: 13,
            RemainingSeats: 5,
            CapacityPercent: 60,
            CapacityExceeded: false,
            Priority: "high",
            Status: "collecting_demand",
            RouteType: "daily",
            LaunchRecommendation: "",
            LaunchReason: "",
            NextAction: "",
            Captain: null,
            Passengers: [],
            PreferredDepartureTimes: [],
            WorkDays: [],
            Readiness: new RouteDemandReadinessDto(
                RouteId: resolvedRouteId,
                RouteName: "A - B",
                VehicleType: "MiniBus",
                VehicleTypeName: "ميني باص",
                Capacity: 13,
                DemandCount: 10,
                UniquePassengers: 8,
                ConfirmedPassengers: 8,
                OccupancyPercent: 60,
                MinimumLaunchRiders: 8,
                TargetOccupancy: 10,
                RidersRequired: 8,
                RemainingToTarget: 0,
                PricingAvailable: pricingLinked,
                PricingLinked: pricingLinked,
                PricingSource: "ROUTE",
                OneWayPrice: 75,
                RoundTripPrice: 150,
                WeeklyPrice: null,
                MonthlyPrice: null,
                CommissionRate: 0,
                CommissionType: "Launch",
                LaunchPeriodDays: 90,
                LaunchStartAt: null,
                LaunchEndAt: null,
                LaunchActive: true,
                FinancialAtMinimumOneWayGross: 600,
                FinancialAtMinimumRoundTripGross: 1200,
                FinancialAtMinimumPlatformCommission: 0,
                FinancialAtMinimumCaptainEarnings: 600,
                LaunchStatus: launchStatus,
                ReasonCode: launchStatus,
                ReadinessReason: "test",
                PricingRuleId: Guid.NewGuid(),
                LastUpdatedAt: DateTime.UtcNow));
    }

    [Fact]
    public void Ready_route_passes_eligibility()
    {
        RouteDemandLaunchEligibility.EnsureReadyToLaunch(Details("READY"));
    }

    [Theory]
    [InlineData("ALMOST_READY")]
    [InlineData("NOT_READY")]
    [InlineData("NO_PRICING")]
    [InlineData("MISSING_ROUTE_LINK")]
    [InlineData("UNKNOWN")]
    [InlineData("MISSING_CONFIGURATION")]
    [InlineData("FULL")]
    public void Non_ready_statuses_are_rejected(string status)
    {
        var ex = Assert.Throws<AppException>(() =>
            RouteDemandLaunchEligibility.EnsureReadyToLaunch(Details(status)));
        Assert.Equal("NOT_READY_TO_LAUNCH", ex.Code);
    }

    [Fact]
    public void Missing_route_link_is_rejected()
    {
        var ex = Assert.Throws<AppException>(() =>
            RouteDemandLaunchEligibility.EnsureReadyToLaunch(
                Details("READY", pricingLinked: false, routeId: null)));
        Assert.Equal("ROUTE_NOT_LINKED", ex.Code);
    }

    [Fact]
    public void Null_details_rejected()
    {
        var ex = Assert.Throws<AppException>(() =>
            RouteDemandLaunchEligibility.EnsureReadyToLaunch(null));
        Assert.Equal("ROUTE_DEMAND_NOT_FOUND", ex.Code);
    }

    [Fact]
    public void Past_scheduled_at_rejected()
    {
        var now = DateTime.UtcNow;
        var ex = Assert.Throws<AppException>(() =>
            RouteDemandLaunchEligibility.EnsureScheduledAtValid(now.AddMinutes(-1), now));
        Assert.Equal("INVALID_SERVICE_DATE", ex.Code);
    }

    [Fact]
    public void Future_scheduled_at_accepted()
    {
        var now = DateTime.UtcNow;
        RouteDemandLaunchEligibility.EnsureScheduledAtValid(now.AddHours(2), now);
    }

    [Fact]
    public void Launch_button_only_for_ready()
    {
        Assert.True(RouteDemandLaunchEligibility.IsLaunchButtonEnabled("READY"));
        Assert.False(RouteDemandLaunchEligibility.IsLaunchButtonEnabled("ALMOST_READY"));
        Assert.False(RouteDemandLaunchEligibility.IsLaunchButtonEnabled(null));
    }
}
