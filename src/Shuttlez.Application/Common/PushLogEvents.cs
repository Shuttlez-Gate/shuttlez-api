namespace Shuttlez.Application.Common;

/// <summary>Structured log event names for Phase 4C hardening — no secrets/tokens.</summary>
public static class PushLogEvents
{
    public const string DeviceRegistered = "DEVICE_REGISTERED";
    public const string DeviceUnregistered = "DEVICE_UNREGISTERED";
    public const string CaptainAssigned = "CAPTAIN_ASSIGNED";
    public const string CaptainUnassigned = "CAPTAIN_UNASSIGNED";
    public const string BookingConfirmedNotification = "BOOKING_CONFIRMED_NOTIFICATION";
    public const string TripStartedNotification = "TRIP_STARTED_NOTIFICATION";
    public const string TripCompletedNotification = "TRIP_COMPLETED_NOTIFICATION";
    public const string TripCancelledNotification = "TRIP_CANCELLED_NOTIFICATION";
    public const string RideAssignedNotification = "RIDE_ASSIGNED_NOTIFICATION";
    public const string FcmSendFailed = "FCM_SEND_FAILED";
    public const string FcmTokenInvalid = "FCM_TOKEN_INVALID";
    public const string FcmNotConfigured = "FCM_NOT_CONFIGURED";
}
