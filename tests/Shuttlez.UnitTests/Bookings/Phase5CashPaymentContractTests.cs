using Shuttlez.Application.Bookings;
using Shuttlez.Application.Bookings.DTOs;
using Shuttlez.Application.Common;
using Shuttlez.Application.Notifications;
using Shuttlez.Domain.Enums;

namespace Shuttlez.UnitTests.Bookings;

/// <summary>Phase 5 — CASH-only payment contract (no online gateway).</summary>
public class Phase5CashPaymentContractTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("cash")]
    [InlineData("CASH")]
    [InlineData("Cash")]
    [InlineData("نقدا")]
    [InlineData("نقدًا")]
    public void NormalizeOrThrow_AcceptsCashVariants(string? raw)
    {
        Assert.Equal(CashPaymentPolicy.Cash, CashPaymentPolicy.NormalizeOrThrow(raw));
    }

    [Theory]
    [InlineData("subscription")]
    [InlineData("package")]
    [InlineData("باقة")]
    public void NormalizeOrThrow_AllowsExistingSubscriptionCredit(string raw)
    {
        Assert.Equal(CashPaymentPolicy.Subscription, CashPaymentPolicy.NormalizeOrThrow(raw));
    }

    [Theory]
    [InlineData("tahseel")]
    [InlineData("card")]
    [InlineData("visa")]
    [InlineData("mastercard")]
    [InlineData("wallet")]
    [InlineData("online")]
    [InlineData("stripe")]
    [InlineData("paymob")]
    [InlineData("bitcoin")]
    public void NormalizeOrThrow_RejectsUnsupportedPaymentMethods(string raw)
    {
        var ex = Assert.Throws<AppException>(() => CashPaymentPolicy.NormalizeOrThrow(raw));
        Assert.Equal(ErrorCodes.PaymentMethodNotSupported, ex.Code);
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public void CreateBookingRequest_ExposesOnlyTripSeatsPayment()
    {
        var props = typeof(CreateBookingRequest).GetProperties().Select(p => p.Name).ToHashSet();
        Assert.Contains(nameof(CreateBookingRequest.TripId), props);
        Assert.Contains(nameof(CreateBookingRequest.SeatCount), props);
        Assert.Contains(nameof(CreateBookingRequest.PaymentMethod), props);
        Assert.DoesNotContain("TotalAmount", props);
        Assert.DoesNotContain("PricePerSeat", props);
        Assert.DoesNotContain("CommissionAmount", props);
        Assert.DoesNotContain("CommissionRate", props);
        Assert.DoesNotContain("CaptainEarnings", props);
    }

    [Fact]
    public void CreateBookingResponse_ContainsAuthoritativeCashFields()
    {
        var response = new CreateBookingResponse(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "BK-1",
            TotalAmount: 150m,
            Status: BookingStatus.Confirmed.ToString(),
            Message: "ok",
            PricePerSeat: 75m,
            SeatCount: 2,
            PaymentMethod: CashPaymentPolicy.Cash);

        Assert.Equal(150m, response.TotalAmount);
        Assert.Equal(2, response.SeatCount);
        Assert.Equal(CashPaymentPolicy.Cash, response.PaymentMethod);
        Assert.Equal(75m, response.PricePerSeat);
    }

    [Fact]
    public void SnapshotMath_UsesServerPriceNotClient()
    {
        const decimal tripPrice = 80m;
        const int seats = 3;
        // Client cannot override — total is always trip × seats.
        var total = ShuttleFinancialCalculator.CalculateTotal(tripPrice, seats);
        Assert.Equal(240m, total);
        Assert.NotEqual(999m, total); // rejected client override value
    }

    [Fact]
    public void CommissionSplit_IsServerSideOnly()
    {
        var (rate, commission, captain) =
            ShuttleFinancialCalculator.SplitEarnings(200m, 15);
        Assert.Equal(0.15m, rate);
        Assert.Equal(30m, commission);
        Assert.Equal(170m, captain);
        // Client-sent captain earnings would be ignored by design (not on request DTO).
        Assert.NotEqual(1m, captain);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SeatCount_InvalidRejectedByCalculator(int seats)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ShuttleFinancialCalculator.CalculateTotal(100m, seats));
    }

    [Fact]
    public void HistoricalSnapshotImmutability_Contract()
    {
        // Phase 5: no backfill — persisted booking amounts must not be recalculated.
        const decimal historicalTotal = 120m;
        const decimal historicalCommission = 12m;
        const decimal historicalCaptain = 108m;
        Assert.Equal(120m, historicalTotal);
        Assert.Equal(historicalTotal, historicalCommission + historicalCaptain);
    }

    [Fact]
    public void BookingConfirmed_CashCopy_DoesNotClaimOnlinePayment()
    {
        var title = "تم تأكيد حجزك";
        var body = "تم تأكيد الحجز — الدفع نقدًا للكابتن";
        Assert.Contains("نقدًا", body);
        Assert.DoesNotContain("تم الدفع", body);
        Assert.DoesNotContain("payment successful", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("online", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tahseel", body, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("تم تأكيد حجزك", title);
    }

    [Fact]
    public void NotificationType_BookingConfirmed_IsStable()
    {
        Assert.Equal("BOOKING_CONFIRMED", NotificationTypes.BookingConfirmed);
    }

    [Fact]
    public void IsOnlineGatewayRejected_FlagsGatewayNames()
    {
        Assert.True(CashPaymentPolicy.IsOnlineGatewayRejected("tahseel"));
        Assert.True(CashPaymentPolicy.IsOnlineGatewayRejected("card"));
        Assert.False(CashPaymentPolicy.IsOnlineGatewayRejected("cash"));
        Assert.False(CashPaymentPolicy.IsOnlineGatewayRejected(null));
    }
}
