using Shuttlez.Application.Trips;
using Shuttlez.Domain.Enums;

namespace Shuttlez.UnitTests.Trips;

public class TripLifecycleRulesTests
{
    [Theory]
    [InlineData(TripStatus.Scheduled, true)]
    [InlineData(TripStatus.DriverAssigned, true)]
    [InlineData(TripStatus.InProgress, false)]
    [InlineData(TripStatus.Completed, false)]
    [InlineData(TripStatus.Cancelled, false)]
    public void CanStart(TripStatus status, bool expected) =>
        Assert.Equal(expected, TripLifecycleRules.CanStart(status));

    [Theory]
    [InlineData(TripStatus.InProgress, true)]
    [InlineData(TripStatus.Scheduled, false)]
    [InlineData(TripStatus.DriverAssigned, false)]
    [InlineData(TripStatus.Completed, false)]
    [InlineData(TripStatus.Cancelled, false)]
    public void CanComplete(TripStatus status, bool expected) =>
        Assert.Equal(expected, TripLifecycleRules.CanComplete(status));

    [Fact]
    public void Idempotent_flags()
    {
        Assert.True(TripLifecycleRules.IsAlreadyStarted(TripStatus.InProgress));
        Assert.True(TripLifecycleRules.IsAlreadyCompleted(TripStatus.Completed));
        Assert.True(TripLifecycleRules.IsTerminalBlocked(TripStatus.Cancelled));
    }
}

public class TripLifecycleErrorCodeStabilityTests
{
    [Fact]
    public void Lifecycle_error_codes_are_stable()
    {
        Assert.Equal("TRIP_NOT_ASSIGNED", Shuttlez.Application.Common.ErrorCodes.TripNotAssigned);
        Assert.Equal("TRIP_NOT_STARTABLE", Shuttlez.Application.Common.ErrorCodes.TripNotStartable);
        Assert.Equal("TRIP_NOT_COMPLETABLE", Shuttlez.Application.Common.ErrorCodes.TripNotCompletable);
        Assert.Equal("TRIP_CANCELLED", Shuttlez.Application.Common.ErrorCodes.TripCancelled);
    }
}
