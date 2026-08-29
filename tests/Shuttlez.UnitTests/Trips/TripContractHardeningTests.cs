using Shuttlez.Application.Common;
using Shuttlez.Application.Trips;
using Shuttlez.Domain.Enums;

namespace Shuttlez.UnitTests.Trips;

public class TripBookabilityTests
{
    [Theory]
    [InlineData(TripStatus.Scheduled, true)]
    [InlineData(TripStatus.DriverAssigned, true)]
    [InlineData(TripStatus.InProgress, false)]
    [InlineData(TripStatus.Completed, false)]
    [InlineData(TripStatus.Cancelled, false)]
    public void Bookable_statuses_match_seat_reservation_gate(TripStatus status, bool expected)
    {
        Assert.Equal(expected, TripBookability.IsBookableStatus(status));
        Assert.Equal(!expected, TripBookability.IsTerminalOrNonBookable(status));
    }
}

public class TripPriceSnapshotTests
{
    [Fact]
    public void Same_price_on_update_is_allowed()
    {
        TripPriceSnapshot.EnsureImmutableOnUpdate(75m, 75m);
    }

    [Fact]
    public void Changed_price_on_update_is_rejected()
    {
        var ex = Assert.Throws<AppException>(() =>
            TripPriceSnapshot.EnsureImmutableOnUpdate(75m, 99m));
        Assert.Equal(ErrorCodes.TripPriceImmutable, ex.Code);
    }
}

public class ErrorCodesStabilityTests
{
    [Fact]
    public void Booking_and_launch_error_codes_are_stable()
    {
        Assert.Equal("SEAT_UNAVAILABLE", ErrorCodes.SeatUnavailable);
        Assert.Equal("TRIP_NOT_BOOKABLE", ErrorCodes.TripNotBookable);
        Assert.Equal("TRIP_PRICE_IMMUTABLE", ErrorCodes.TripPriceImmutable);
        Assert.Equal("NOT_READY_TO_LAUNCH", ErrorCodes.NotReadyToLaunch);
        Assert.Equal("DUPLICATE_OPERATIONAL_TRIP", ErrorCodes.DuplicateOperationalTrip);
        Assert.Equal("PRICING_NOT_CONFIGURED", ErrorCodes.PricingNotConfigured);
        Assert.Equal("INVALID_SERVICE_DATE", ErrorCodes.InvalidServiceDate);
    }
}
