using Shuttlez.Application.Subscriptions;

namespace Shuttlez.UnitTests.Subscriptions;

public class SubscriptionUsageCalculatorTests
{
    [Theory]
    [InlineData(30, 7, 23)]
    [InlineData(30, 30, 0)]
    [InlineData(30, 40, 0)]
    [InlineData(0, 5, int.MaxValue)]
    public void ComputeRemaining_MatchesPurchasedMinusUsed(int purchased, int used, int expected)
    {
        Assert.Equal(expected, SubscriptionUsageCalculator.ComputeRemaining(purchased, used));
    }

    [Fact]
    public void Resolve_ExplicitCash_NeverUsesCredit()
    {
        var d = SubscriptionUsageCalculator.Resolve(
            paymentMethod: "cash",
            hasActivePackage: true,
            isExpired: false,
            packageTripCount: 30,
            usedSeatCredits: 0,
            seatCount: 1);
        Assert.Equal(SubscriptionCreditDecision.CashExplicit, d);
    }

    [Fact]
    public void Resolve_SubscriptionExpired_ReturnsExpired()
    {
        var d = SubscriptionUsageCalculator.Resolve(
            paymentMethod: "subscription",
            hasActivePackage: true,
            isExpired: true,
            packageTripCount: 30,
            usedSeatCredits: 0,
            seatCount: 1);
        Assert.Equal(SubscriptionCreditDecision.Expired, d);
    }

    [Fact]
    public void Resolve_SubscriptionExhausted_ReturnsLimitReached_NotCash()
    {
        var d = SubscriptionUsageCalculator.Resolve(
            paymentMethod: "subscription",
            hasActivePackage: true,
            isExpired: false,
            packageTripCount: 10,
            usedSeatCredits: 10,
            seatCount: 1);
        Assert.Equal(SubscriptionCreditDecision.LimitReached, d);
    }

    [Fact]
    public void Resolve_SubscriptionRemainingOne_AllowsCredit()
    {
        var d = SubscriptionUsageCalculator.Resolve(
            paymentMethod: "subscription",
            hasActivePackage: true,
            isExpired: false,
            packageTripCount: 10,
            usedSeatCredits: 9,
            seatCount: 1);
        Assert.Equal(SubscriptionCreditDecision.UseCredit, d);
    }

    [Fact]
    public void Resolve_Unlimited_UsesCredit()
    {
        var d = SubscriptionUsageCalculator.Resolve(
            paymentMethod: "subscription",
            hasActivePackage: true,
            isExpired: false,
            packageTripCount: 0,
            usedSeatCredits: 100,
            seatCount: 2);
        Assert.Equal(SubscriptionCreditDecision.UseCredit, d);
    }

    [Theory]
    [InlineData(100, 1, 100)]
    [InlineData(100, 3, 300)]
    public void ServerTotal_IsPriceTimesSeats(decimal price, int seats, decimal expected)
    {
        Assert.Equal(
            expected,
            Shuttlez.Application.Bookings.ShuttleFinancialCalculator.CalculateTotal(price, seats));
    }

    [Fact]
    public void ServerCommission_LaunchZeroPlatform()
    {
        var (rate, commission, captain) =
            Shuttlez.Application.Bookings.ShuttleFinancialCalculator.SplitEarnings(300, 0);
        Assert.Equal(0m, rate);
        Assert.Equal(0m, commission);
        Assert.Equal(300m, captain);
    }
}
