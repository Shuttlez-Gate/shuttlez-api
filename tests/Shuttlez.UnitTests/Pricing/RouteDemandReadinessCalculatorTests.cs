using Shuttlez.Application.Admin.Services;
using Shuttlez.Application.Bookings;
using Shuttlez.Domain.Enums;

namespace Shuttlez.UnitTests.Pricing;

public class RouteDemandReadinessCalculatorTests
{
    private static readonly Guid RouteId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid PricingId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static RouteDemandReadinessInput Linked(
        int confirmed = 7,
        int demand = 8,
        int unique = 7,
        int capacity = 13,
        int? min = 8,
        int? target = 10,
        decimal? oneWay = 75,
        decimal? roundTrip = 150,
        decimal commission = 0,
        Guid? pricingId = null,
        bool hasPrices = true) =>
        new(
            demand,
            confirmed,
            unique,
            RouteId,
            RouteMatchSafe: true,
            VehicleType.MiniBus,
            capacity,
            hasPrices ? (pricingId ?? PricingId) : null,
            hasPrices ? "ROUTE_VEHICLE" : null,
            hasPrices ? oneWay : null,
            hasPrices ? roundTrip : null,
            hasPrices ? 690m : null,
            hasPrices ? 2850m : null,
            min,
            target,
            hasPrices ? commission : null,
            hasPrices ? "LAUNCH" : null,
            hasPrices ? 90 : null,
            hasPrices ? DateTime.UtcNow.Date : null,
            hasPrices ? DateTime.UtcNow.Date.AddDays(90) : null,
            hasPrices ? true : null);

    [Fact]
    public void MissingRouteId_Unknown_MissingRouteLink()
    {
        var input = Linked() with { RouteId = null, RouteMatchSafe = false };
        var r = RouteDemandReadinessCalculator.Evaluate(input);
        Assert.Equal(DemandLaunchReadinessStatus.Unknown, r.LaunchStatus);
        Assert.Equal(DemandLaunchReasonCodes.MissingRouteLink, r.ReasonCode);
        Assert.False(r.PricingAvailable);
        Assert.Null(r.OneWayPrice);
        Assert.Null(r.FinancialAtMinimumRoundTripGross);
    }

    [Fact]
    public void NoPricingRule_NoPricing()
    {
        var r = RouteDemandReadinessCalculator.Evaluate(Linked(hasPrices: false));
        Assert.Equal(DemandLaunchReadinessStatus.NoPricing, r.LaunchStatus);
        Assert.Equal(DemandLaunchReasonCodes.NoPricing, r.ReasonCode);
    }

    [Fact]
    public void MissingMinimum_MissingConfiguration()
    {
        var r = RouteDemandReadinessCalculator.Evaluate(Linked(min: null, target: 10));
        Assert.Equal(DemandLaunchReadinessStatus.MissingConfiguration, r.LaunchStatus);
    }

    [Fact]
    public void MissingTarget_MissingConfiguration()
    {
        var r = RouteDemandReadinessCalculator.Evaluate(Linked(min: 8, target: null));
        Assert.Equal(DemandLaunchReadinessStatus.MissingConfiguration, r.LaunchStatus);
    }

    [Fact]
    public void CapacityNull_Unknown()
    {
        var r = RouteDemandReadinessCalculator.Evaluate(Linked() with { Capacity = null });
        Assert.Equal(DemandLaunchReadinessStatus.Unknown, r.LaunchStatus);
        Assert.Equal(DemandLaunchReasonCodes.MissingCapacity, r.ReasonCode);
    }

    [Fact]
    public void ConfirmedBelowMinimum_NotReady()
    {
        var r = RouteDemandReadinessCalculator.Evaluate(Linked(confirmed: 7, min: 8, target: 10));
        Assert.Equal(DemandLaunchReadinessStatus.NotReady, r.LaunchStatus);
        Assert.Equal(1, r.RidersRequired);
        Assert.Equal(DemandLaunchReasonCodes.BelowMinimum, r.ReasonCode);
    }

    [Fact]
    public void ConfirmedEqualsMinimum_AlmostReady_WhenTargetHigher()
    {
        var r = RouteDemandReadinessCalculator.Evaluate(Linked(confirmed: 8, min: 8, target: 10));
        Assert.Equal(DemandLaunchReadinessStatus.AlmostReady, r.LaunchStatus);
        Assert.Equal(0, r.RidersRequired);
        Assert.Equal(2, r.RemainingToTarget);
    }

    [Fact]
    public void ConfirmedEqualsMinimum_Ready_WhenTargetEqualsMinimum()
    {
        var r = RouteDemandReadinessCalculator.Evaluate(Linked(confirmed: 8, min: 8, target: 8));
        Assert.Equal(DemandLaunchReadinessStatus.Ready, r.LaunchStatus);
    }

    [Fact]
    public void ConfirmedMeetsTarget_Ready()
    {
        var r = RouteDemandReadinessCalculator.Evaluate(Linked(confirmed: 10, min: 8, target: 10));
        Assert.Equal(DemandLaunchReadinessStatus.Ready, r.LaunchStatus);
        Assert.Equal(0, r.RidersRequired);
    }

    [Fact]
    public void Occupancy_CarShuttle_UsesAuthoritativeCapacityFour_NotMarketingThree()
    {
        // Demand band may show 3 seats; authoritative CarShuttle capacity is 4 → 3 confirmed = 75%.
        var r = RouteDemandReadinessCalculator.Evaluate(
            Linked(confirmed: 3, demand: 3, unique: 3, capacity: 4, min: 3, target: 4, oneWay: 120, roundTrip: 240));
        Assert.Equal(
            Shuttlez.Application.Pricing.ShuttlePricingCalculator.OccupancyPercent(3, 4),
            r.OccupancyPercent);
        Assert.Equal(75m, r.OccupancyPercent);
    }

    [Fact]
    public void Occupancy_UsesConfirmedNotDemand()
    {
        var r = RouteDemandReadinessCalculator.Evaluate(
            Linked(confirmed: 7, demand: 8, capacity: 13, min: 8, target: 10));
        // 7/13 → AwayFromZero 2dp
        Assert.Equal(
            Shuttlez.Application.Pricing.ShuttlePricingCalculator.OccupancyPercent(7, 13),
            r.OccupancyPercent);
        Assert.NotEqual(
            Shuttlez.Application.Pricing.ShuttlePricingCalculator.OccupancyPercent(8, 13),
            r.OccupancyPercent);
    }

    [Fact]
    public void FinancialPreview_AtMinimumLaunchOnly_ZeroCommission()
    {
        var r = RouteDemandReadinessCalculator.Evaluate(Linked(confirmed: 7, min: 8, commission: 0));
        Assert.Equal(600m, r.FinancialAtMinimumOneWayGross); // 8×75
        Assert.Equal(1200m, r.FinancialAtMinimumRoundTripGross); // 8×150
        Assert.Equal(0m, r.FinancialAtMinimumPlatformCommission);
        Assert.Equal(1200m, r.FinancialAtMinimumCaptainEarnings);
    }

    [Fact]
    public void FinancialPreview_PermanentTenPercent()
    {
        var r = RouteDemandReadinessCalculator.Evaluate(Linked(confirmed: 10, min: 8, commission: 10));
        Assert.Equal(1200m, r.FinancialAtMinimumRoundTripGross);
        Assert.Equal(120m, r.FinancialAtMinimumPlatformCommission);
        Assert.Equal(1080m, r.FinancialAtMinimumCaptainEarnings);
    }

    [Fact]
    public void TargetExceedsCapacity_MissingConfiguration()
    {
        var r = RouteDemandReadinessCalculator.Evaluate(Linked(target: 20, capacity: 13));
        Assert.Equal(DemandLaunchReadinessStatus.MissingConfiguration, r.LaunchStatus);
        Assert.Equal(DemandLaunchReasonCodes.InvalidLaunchConfig, r.ReasonCode);
    }

    [Fact]
    public void HistoricalSnapshot_Unaffected()
    {
        var (_, c, captain) = ShuttleFinancialCalculator.SplitEarnings(750m, 0);
        var estimate = RouteDemandReadinessCalculator.Evaluate(Linked(oneWay: 99, commission: 10));
        Assert.Equal(0m, c);
        Assert.Equal(750m, captain);
        Assert.NotEqual(750m, estimate.FinancialAtMinimumOneWayGross);
    }

    [Fact]
    public void ApiStatusStrings()
    {
        Assert.Equal("READY", RouteDemandReadinessCalculator.ToApiStatus(DemandLaunchReadinessStatus.Ready));
        Assert.Equal("NO_PRICING", RouteDemandReadinessCalculator.ToApiStatus(DemandLaunchReadinessStatus.NoPricing));
        Assert.Equal("UNKNOWN", RouteDemandReadinessCalculator.ToApiStatus(DemandLaunchReadinessStatus.Unknown));
        Assert.Equal("NOT_READY", RouteDemandReadinessCalculator.ToApiStatus(DemandLaunchReadinessStatus.NotReady));
        Assert.Equal(
            "MISSING_CONFIGURATION",
            RouteDemandReadinessCalculator.ToApiStatus(DemandLaunchReadinessStatus.MissingConfiguration));
    }

    [Fact]
    public void ExactBidirectionalMatch_AcceptsReversedEndpoints_NoFuzzy()
    {
        var key = RouteLocationNormalizer.BuildRouteKey(
            RouteLocationNormalizer.Normalize("حلوان"),
            RouteLocationNormalizer.Normalize("مصر الجديدة"));
        Assert.True(RouteDemandReadinessEnricher.IsExactBidirectionalMatch(key, "حلوان - مصر الجديدة"));
        Assert.True(RouteDemandReadinessEnricher.IsExactBidirectionalMatch(key, "مصر الجديدة - حلوان"));
        Assert.False(RouteDemandReadinessEnricher.IsExactBidirectionalMatch(key, "حلوان، القاهرة - مصر الجديدة، القاهرة"));
        Assert.False(RouteDemandReadinessEnricher.IsExactBidirectionalMatch(key, "حلوان"));
    }

    [Fact]
    public void ExplicitMappedRoute_TakesPriorityOverExactKey()
    {
        var exactId = Guid.NewGuid();
        var mappedId = Guid.NewGuid();
        var key = RouteLocationNormalizer.BuildRouteKey(
            RouteLocationNormalizer.Normalize("حلوان"),
            RouteLocationNormalizer.Normalize("مصر الجديدة"));
        var index = RouteDemandReadinessEnricher.BuildRouteKeyIndex([(exactId, "حلوان - مصر الجديدة")]);
        var (routeId, safe, ambiguous, source) = RouteDemandReadinessEnricher.ResolveRouteLink(
            mappedId,
            key,
            new HashSet<Guid> { exactId, mappedId },
            index);
        Assert.True(safe);
        Assert.False(ambiguous);
        Assert.Equal(mappedId, routeId);
        Assert.Equal("EXPLICIT", source);
    }

    [Fact]
    public void ExplicitMappedRoute_StaleId_NotSafe_NoFuzzyFallback()
    {
        var exactId = Guid.NewGuid();
        var staleMapped = Guid.NewGuid();
        var key = RouteLocationNormalizer.BuildRouteKey(
            RouteLocationNormalizer.Normalize("حلوان"),
            RouteLocationNormalizer.Normalize("مصر الجديدة"));
        var index = RouteDemandReadinessEnricher.BuildRouteKeyIndex([(exactId, "حلوان - مصر الجديدة")]);
        var (routeId, safe, ambiguous, source) = RouteDemandReadinessEnricher.ResolveRouteLink(
            staleMapped,
            key,
            new HashSet<Guid> { exactId },
            index);
        Assert.False(safe);
        Assert.Null(routeId);
        Assert.Null(source);
        Assert.False(ambiguous);
    }

    [Fact]
    public void ExactRouteKeyMatch_Only()
    {
        var id = Guid.NewGuid();
        var map = RouteDemandReadinessEnricher.BuildRouteKeyIndex(
        [
            (id, "حلوان - مصر الجديدة"),
            (Guid.NewGuid(), "الشروق - مدينة نصر"),
        ]);
        var key = RouteLocationNormalizer.BuildRouteKey(
            RouteLocationNormalizer.Normalize("حلوان"),
            RouteLocationNormalizer.Normalize("مصر الجديدة"));
        Assert.True(map.TryGetValue(key, out var hits));
        Assert.Single(hits);
        Assert.Equal(id, hits[0]);
    }

    [Fact]
    public void AmbiguousExactKey_NotSafeSingleMatch()
    {
        var map = RouteDemandReadinessEnricher.BuildRouteKeyIndex(
        [
            (Guid.NewGuid(), "حلوان - مصر الجديدة"),
            (Guid.NewGuid(), "مصر الجديدة - حلوان"),
        ]);
        var key = RouteLocationNormalizer.BuildRouteKey(
            RouteLocationNormalizer.Normalize("حلوان"),
            RouteLocationNormalizer.Normalize("مصر الجديدة"));
        Assert.True(map.TryGetValue(key, out var hits));
        Assert.Equal(2, hits.Count);
    }

    [Fact]
    public void VehicleSpecificOverridesDefault_PickOrder()
    {
        var routeId = Guid.NewGuid();
        var routeRule = new Domain.Entities.PricingRule
        {
            Id = Guid.NewGuid(),
            RouteId = routeId,
            VehicleType = VehicleType.MiniBus,
            OneWayPrice = 80,
            RoundTripPrice = 160,
            EffectiveFrom = DateTime.UtcNow.AddDays(-1),
        };
        var defaultRule = new Domain.Entities.PricingRule
        {
            Id = Guid.NewGuid(),
            RouteId = null,
            VehicleType = VehicleType.MiniBus,
            OneWayPrice = 75,
            RoundTripPrice = 150,
            EffectiveFrom = DateTime.UtcNow.AddDays(-2),
        };
        var picked = RouteDemandReadinessEnricher.PickPricingRule([defaultRule, routeRule], routeId, VehicleType.MiniBus);
        Assert.Equal(routeRule.Id, picked!.Id);
        Assert.Equal(80m, picked.OneWayPrice);
    }
}
