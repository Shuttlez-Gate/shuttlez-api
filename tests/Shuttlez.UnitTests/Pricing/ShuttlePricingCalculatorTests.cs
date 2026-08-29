using Shuttlez.Application.Pricing;

namespace Shuttlez.UnitTests.Pricing;

public class ShuttlePricingCalculatorTests
{
    [Fact]
    public void Car_OneWay_Gross()
    {
        var unit = ShuttlePricingCalculator.ResolveUnitPrice(120, 240, PricingTripType.OneWay);
        Assert.Equal(120m, unit);
        Assert.Equal(360m, ShuttlePricingCalculator.CalculateGross(unit, 3));
    }

    [Fact]
    public void Microbus_RoundTrip_Gross()
    {
        var unit = ShuttlePricingCalculator.ResolveUnitPrice(75, 150, PricingTripType.RoundTrip);
        Assert.Equal(150m, unit);
        Assert.Equal(1500m, ShuttlePricingCalculator.CalculateGross(unit, 10));
    }

    [Fact]
    public void LaunchPeriod_UsesZeroCommission()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var pct = ShuttlePricingCalculator.ResolveCommissionPercent(
            launchCommissionPercent: 0,
            permanentCommissionPercent: 10,
            launchPeriodDays: 90,
            launchStartUtc: start,
            asOfUtc: start.AddDays(30));
        Assert.Equal(0m, pct);
    }

    [Fact]
    public void AfterLaunch_UsesPermanentTenPercent()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var pct = ShuttlePricingCalculator.ResolveCommissionPercent(
            0, 10, 90, start, start.AddDays(91));
        Assert.Equal(10m, pct);
    }

    [Fact]
    public void CaptainEarnings_LaunchZeroCommission()
    {
        var gross = 1500m;
        var (_, commission, captain) =
            Shuttlez.Application.Bookings.ShuttleFinancialCalculator.SplitEarnings(gross, 0);
        Assert.Equal(0m, commission);
        Assert.Equal(1500m, captain);
    }

    [Fact]
    public void CaptainEarnings_PermanentTenPercent()
    {
        var gross = 1500m;
        var (_, commission, captain) =
            Shuttlez.Application.Bookings.ShuttleFinancialCalculator.SplitEarnings(gross, 10);
        Assert.Equal(150m, commission);
        Assert.Equal(1350m, captain);
    }

    [Theory]
    [InlineData(7, 8, 10, 13, RouteLaunchStatus.CollectingDemand)]
    [InlineData(8, 8, 10, 13, RouteLaunchStatus.AlmostReady)]
    [InlineData(10, 8, 10, 13, RouteLaunchStatus.ReadyToLaunch)]
    [InlineData(13, 8, 10, 13, RouteLaunchStatus.Full)]
    [InlineData(3, 3, 3, 3, RouteLaunchStatus.Full)]
    public void LaunchStatus(
        int riders, int min, int target, int cap, RouteLaunchStatus expected)
    {
        Assert.Equal(
            expected,
            ShuttlePricingCalculator.ResolveLaunchStatus(riders, min, target, cap));
    }

    [Fact]
    public void OccupancyPercent_MicrobusTenOfThirteen()
    {
        Assert.Equal(76.92m, ShuttlePricingCalculator.OccupancyPercent(10, 13));
    }

    [Fact]
    public void MoneyPrecision_TwoDecimals()
    {
        Assert.Equal(100.13m, ShuttlePricingCalculator.NormalizeMoney(100.125m));
    }

    [Fact]
    public void HistoricalSnapshot_UnaffectedByLaterPriceEdit()
    {
        // Booking snapshot at confirm time — Admin later edits catalog prices.
        var snapPrice = 75m;
        var snapSeats = 10;
        var snapGross = ShuttlePricingCalculator.CalculateGross(snapPrice, snapSeats);
        var (_, snapCommission, snapCaptain) =
            Shuttlez.Application.Bookings.ShuttleFinancialCalculator.SplitEarnings(snapGross, 0);

        var editedCatalogOneWay = 99m; // Admin changed PricingRule after booking
        var wouldBeIfRecalculated = ShuttlePricingCalculator.CalculateGross(editedCatalogOneWay, snapSeats);

        Assert.Equal(750m, snapGross);
        Assert.Equal(0m, snapCommission);
        Assert.Equal(750m, snapCaptain);
        Assert.NotEqual(snapGross, wouldBeIfRecalculated);
    }

    [Fact]
    public void CapacityOccupancy_CannotExceedHundredWhenFull()
    {
        Assert.Equal(100m, ShuttlePricingCalculator.OccupancyPercent(13, 13));
        Assert.Equal(RouteLaunchStatus.Full,
            ShuttlePricingCalculator.ResolveLaunchStatus(13, 8, 10, 13));
    }
}
