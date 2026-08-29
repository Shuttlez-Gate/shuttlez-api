using Shuttlez.Application.Bookings;

namespace Shuttlez.UnitTests.Bookings;

public class ShuttleFinancialCalculatorTests
{
    [Theory]
    [InlineData(100, 1, 100)]
    [InlineData(100, 2, 200)]
    [InlineData(80.5, 3, 241.5)]
    public void CalculateTotal_MultipliesAndRounds(decimal price, int seats, decimal expected)
    {
        var total = ShuttleFinancialCalculator.CalculateTotal(price, seats);
        Assert.Equal(expected, total);
    }

    [Fact]
    public void CalculateTotal_RejectsInvalidSeatCount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ShuttleFinancialCalculator.CalculateTotal(100, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ShuttleFinancialCalculator.CalculateTotal(100, -1));
    }

    [Theory]
    [InlineData(100, 0, 0, 0, 100)]
    [InlineData(100, 10, 0.10, 10, 90)]
    [InlineData(100, 100, 1.00, 100, 0)]
    [InlineData(200, 15, 0.15, 30, 170)]
    public void SplitEarnings_AppliesPlatformPercent(
        decimal total,
        decimal platformPercent,
        decimal expectedRate,
        decimal expectedCommission,
        decimal expectedCaptain)
    {
        var (rate, commission, captain) =
            ShuttleFinancialCalculator.SplitEarnings(total, platformPercent);
        Assert.Equal(expectedRate, rate);
        Assert.Equal(expectedCommission, commission);
        Assert.Equal(expectedCaptain, captain);
        Assert.Equal(total, commission + captain);
    }

    [Fact]
    public void SplitEarnings_ClampsPercent()
    {
        var (_, commission, captain) = ShuttleFinancialCalculator.SplitEarnings(100, 150);
        Assert.Equal(100m, commission);
        Assert.Equal(0m, captain);
    }
}
