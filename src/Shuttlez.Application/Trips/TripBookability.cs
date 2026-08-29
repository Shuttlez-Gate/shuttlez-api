using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Trips;

/// <summary>
/// Shared bookability rules for rider booking and seat reservation.
/// Status names match <see cref="TripStatus"/> — do not invent a parallel enum.
/// </summary>
public static class TripBookability
{
    /// <summary>
    /// Statuses that may accept new rider bookings (also enforced in TryDecrementTripSeatsAsync).
    /// </summary>
    public static bool IsBookableStatus(TripStatus status) =>
        status is TripStatus.Scheduled or TripStatus.DriverAssigned;

    public static bool IsTerminalOrNonBookable(TripStatus status) =>
        status is TripStatus.Cancelled or TripStatus.Completed or TripStatus.InProgress;
}
