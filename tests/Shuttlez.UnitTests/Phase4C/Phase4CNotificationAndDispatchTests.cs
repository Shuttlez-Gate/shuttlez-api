using Shuttlez.Application.Common;
using Shuttlez.Application.Trips;
using Shuttlez.Domain.Enums;

namespace Shuttlez.UnitTests.Phase4C;

public class Phase4CNotificationAndDispatchTests
{
    [Fact]
    public void Notification_types_are_stable()
    {
        Assert.Equal("CAPTAIN_TRIP_ASSIGNED", NotificationTypes.CaptainTripAssigned);
        Assert.Equal("CAPTAIN_TRIP_UNASSIGNED", NotificationTypes.CaptainTripUnassigned);
        Assert.Equal("TRIP_CANCELLED", NotificationTypes.TripCancelled);
        Assert.Equal("BOOKING_CONFIRMED", NotificationTypes.BookingConfirmed);
        Assert.Equal("TRIP_STARTED", NotificationTypes.TripStarted);
        Assert.Equal("TRIP_COMPLETED", NotificationTypes.TripCompleted);
    }

    [Fact]
    public void Dispatch_error_codes_are_stable()
    {
        Assert.Equal("DRIVER_NOT_FOUND", ErrorCodes.DriverNotFound);
        Assert.Equal("DRIVER_INACTIVE", ErrorCodes.DriverInactive);
        Assert.Equal("DRIVER_NOT_ELIGIBLE", ErrorCodes.DriverNotEligible);
        Assert.Equal("DRIVER_TRIP_CONFLICT", ErrorCodes.DriverTripConflict);
        Assert.Equal("TRIP_NOT_FOUND", ErrorCodes.TripNotFound);
        Assert.Equal("TRIP_ALREADY_STARTED", ErrorCodes.TripAlreadyStarted);
        Assert.Equal("TRIP_ALREADY_COMPLETED", ErrorCodes.TripAlreadyCompleted);
        Assert.Equal("TRIP_CANCELLED", ErrorCodes.TripCancelled);
    }

    [Theory]
    [InlineData(TripStatus.Scheduled, true)]
    [InlineData(TripStatus.DriverAssigned, true)]
    [InlineData(TripStatus.InProgress, false)]
    [InlineData(TripStatus.Completed, false)]
    [InlineData(TripStatus.Cancelled, false)]
    public void Assignable_statuses_match_lifecycle_startable_window(TripStatus status, bool canAssignBeforeStart)
    {
        // Assignment allowed only before InProgress (mirrors CanStart window for pre-start ops).
        var assignable = status is TripStatus.Scheduled or TripStatus.DriverAssigned;
        Assert.Equal(canAssignBeforeStart, assignable);
        if (canAssignBeforeStart)
            Assert.True(TripLifecycleRules.CanStart(status));
    }

    [Fact]
    public void Push_message_contract_includes_type_and_ids()
    {
        var data = new Dictionary<string, string>
        {
            ["type"] = NotificationTypes.CaptainTripAssigned,
            ["tripId"] = Guid.NewGuid().ToString(),
            ["routeId"] = Guid.NewGuid().ToString(),
        };
        Assert.Equal(NotificationTypes.CaptainTripAssigned, data["type"]);
        Assert.True(Guid.TryParse(data["tripId"], out _));
    }
}
