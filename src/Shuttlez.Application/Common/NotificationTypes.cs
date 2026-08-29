namespace Shuttlez.Application.Common;

public static class NotificationTypes
{
    public const string CaptainTripAssigned = "CAPTAIN_TRIP_ASSIGNED";
    public const string CaptainTripUnassigned = "CAPTAIN_TRIP_UNASSIGNED";
    public const string TripCancelled = "TRIP_CANCELLED";
    public const string BookingConfirmed = "BOOKING_CONFIRMED";
    public const string TripStarted = "TRIP_STARTED";
    public const string TripCompleted = "TRIP_COMPLETED";

    /// <summary>Rider Direct Ride — Captain assigned (Phase 6K). Payload: rideId.</summary>
    public const string RideAssigned = "RIDE_ASSIGNED";
}
