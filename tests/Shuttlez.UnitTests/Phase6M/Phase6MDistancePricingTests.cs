using Shuttlez.Application.Bookings;
using Shuttlez.Application.Common;
using Shuttlez.Application.Rides;
using Shuttlez.Domain.Entities;

namespace Shuttlez.UnitTests.Phase6M;

public class DistanceBasedFareCalculatorTests
{
    [Fact]
    public void CalculateRideFare_UsesBasePlusDistanceTimesRate()
    {
        var rule = new RideFareRule
        {
            BaseFare = 5m,
            PricePerKm = 2m,
            FlatFare = 999m
        };

        var fare = DistanceBasedFareCalculator.CalculateRideFare(rule, 10m);

        Assert.Equal(25m, fare);
    }

    [Fact]
    public void CalculateRideFare_AppliesMinimumFare()
    {
        var rule = new RideFareRule
        {
            BaseFare = 0m,
            PricePerKm = 1m,
            MinimumFare = 20m,
            FlatFare = 999m
        };

        var fare = DistanceBasedFareCalculator.CalculateRideFare(rule, 5m);

        Assert.Equal(20m, fare);
    }

    [Fact]
    public void CalculateRideFare_AppliesMaximumFare()
    {
        var rule = new RideFareRule
        {
            BaseFare = 0m,
            PricePerKm = 10m,
            MaximumFare = 50m,
            FlatFare = 999m
        };

        var fare = DistanceBasedFareCalculator.CalculateRideFare(rule, 10m);

        Assert.Equal(50m, fare);
    }

    [Fact]
    public void CalculateRideFare_UsesLegacyFlatFareWhenPricePerKmZero()
    {
        var rule = new RideFareRule
        {
            FlatFare = 150m,
            PricePerKm = 0m,
            BaseFare = 99m
        };

        var fare = DistanceBasedFareCalculator.CalculateRideFare(rule, 100m);

        Assert.Equal(150m, fare);
    }

    [Fact]
    public void CalculateGroupFare_UsesLegacyCharterFlatFareWhenPricePerKmZero()
    {
        var rule = new GroupFareRule
        {
            CharterFlatFare = 400m,
            PricePerKm = 0m,
            BaseFare = 50m
        };

        var fare = DistanceBasedFareCalculator.CalculateGroupFare(rule, 25m);

        Assert.Equal(400m, fare);
    }

    [Fact]
    public void NormalizeMoney_RoundsToTwoDecimals()
    {
        var normalized = ShuttleFinancialCalculator.NormalizeMoney(12.345m);
        Assert.Equal(12.35m, normalized);
    }
}

public class Phase6MContractTests
{
    [Fact]
    public void RideRequest_AllowsNullDistanceKmForHistoricalRows()
    {
        var ride = new RideRequest();
        Assert.Null(ride.DistanceKm);
        Assert.Null(ride.BaseFareApplied);
        Assert.Null(ride.PricePerKmApplied);
        Assert.Null(ride.MinimumFareApplied);
    }

    [Fact]
    public void RideFareRule_NewDistanceFieldsDefaultToZero()
    {
        var rule = new RideFareRule();
        Assert.Equal(0m, rule.BaseFare);
        Assert.Equal(0m, rule.PricePerKm);
        Assert.Null(rule.MinimumFare);
        Assert.Null(rule.MaximumFare);
        Assert.Equal(0m, rule.FlatFare);
    }

    [Fact]
    public void GroupFareRule_NewDistanceFieldsDefaultToZero()
    {
        var rule = new GroupFareRule();
        Assert.Equal(0m, rule.BaseFare);
        Assert.Equal(0m, rule.PricePerKm);
        Assert.Null(rule.MinimumFare);
        Assert.Null(rule.MaximumFare);
        Assert.Equal(0m, rule.CharterFlatFare);
    }

    [Theory]
    [InlineData("tahseel")]
    [InlineData("card")]
    [InlineData("wallet")]
    public void CashPolicy_StillRejectsNonCashForRideGroup(string raw)
    {
        var ex = Assert.Throws<AppException>(() => CashPaymentPolicy.NormalizeOrThrow(raw));
        Assert.Equal(ErrorCodes.PaymentMethodNotSupported, ex.Code);
    }

    [Fact]
    public void ErrorCodes_Phase6M_AreStable()
    {
        Assert.Equal("TRIP_DISTANCE_NOT_CALCULATED", ErrorCodes.TripDistanceNotCalculated);
        Assert.Equal("TRIP_COORDINATES_REQUIRED", ErrorCodes.TripCoordinatesRequired);
    }
}
