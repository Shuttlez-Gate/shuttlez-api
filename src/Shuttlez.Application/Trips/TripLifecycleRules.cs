using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Trips;

/// <summary>
/// Captain Start/Complete eligibility — mirrors existing TripStatus names only.
/// </summary>
public static class TripLifecycleRules
{
    public static bool CanStart(TripStatus status) =>
        status is TripStatus.Scheduled or TripStatus.DriverAssigned;

    public static bool IsAlreadyStarted(TripStatus status) =>
        status == TripStatus.InProgress;

    public static bool CanComplete(TripStatus status) =>
        status == TripStatus.InProgress;

    public static bool IsAlreadyCompleted(TripStatus status) =>
        status == TripStatus.Completed;

    public static bool IsTerminalBlocked(TripStatus status) =>
        status is TripStatus.Cancelled or TripStatus.Completed;
}
