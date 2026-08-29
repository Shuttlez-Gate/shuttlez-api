namespace Shuttlez.Application.Common;

/// Machine-readable business error codes for clients.
public static class ErrorCodes
{
    public const string SeatUnavailable = "SEAT_UNAVAILABLE";
    public const string SubscriptionExpired = "SUBSCRIPTION_EXPIRED";
    public const string SubscriptionLimitReached = "SUBSCRIPTION_LIMIT_REACHED";
    public const string InvalidSeatCount = "INVALID_SEAT_COUNT";
    public const string PricingNotAvailable = "PRICING_NOT_AVAILABLE";
    public const string TripNotBookable = "TRIP_NOT_BOOKABLE";
    public const string DuplicateBooking = "DUPLICATE_BOOKING";

    /// <summary>Admin attempted to change Trip.PricePerSeat after create.</summary>
    public const string TripPriceImmutable = "TRIP_PRICE_IMMUTABLE";

    // Route-demand launch (Phase 3) — stable codes for Admin + mobile contract docs
    public const string RouteDemandNotFound = "ROUTE_DEMAND_NOT_FOUND";
    public const string RouteNotFound = "ROUTE_NOT_FOUND";
    public const string RouteNotLinked = "ROUTE_NOT_LINKED";
    public const string NotReadyToLaunch = "NOT_READY_TO_LAUNCH";
    public const string PricingNotConfigured = "PRICING_NOT_CONFIGURED";
    public const string NoVehicleConfig = "NO_VEHICLE_CONFIG";
    public const string NoVehicleAvailable = "NO_VEHICLE_AVAILABLE";
    public const string InvalidServiceDate = "INVALID_SERVICE_DATE";
    public const string DuplicateOperationalTrip = "DUPLICATE_OPERATIONAL_TRIP";
    public const string DriverNotFound = "DRIVER_NOT_FOUND";
    public const string DriverInactive = "DRIVER_INACTIVE";
    public const string DriverNotEligible = "DRIVER_NOT_ELIGIBLE";
    public const string DriverTripConflict = "DRIVER_TRIP_CONFLICT";
    public const string TripNotFound = "TRIP_NOT_FOUND";
    public const string TripNotAssignable = "TRIP_NOT_ASSIGNABLE";
    public const string DriverAssignmentFailed = "DRIVER_ASSIGNMENT_FAILED";

    // Captain trip lifecycle (Phase 4B)
    public const string TripNotAssigned = "TRIP_NOT_ASSIGNED";
    public const string TripNotStartable = "TRIP_NOT_STARTABLE";
    public const string TripNotCompletable = "TRIP_NOT_COMPLETABLE";
    public const string TripAlreadyStarted = "TRIP_ALREADY_STARTED";
    public const string TripAlreadyCompleted = "TRIP_ALREADY_COMPLETED";
    public const string TripCancelled = "TRIP_CANCELLED";
    public const string InvalidTripStatus = "INVALID_TRIP_STATUS";

    /// <summary>Client requested unsupported payment method (Phase 5 — CASH only for money).</summary>
    public const string PaymentMethodNotSupported = "PAYMENT_METHOD_NOT_SUPPORTED";

    /// <summary>
    /// Phase 6A — legacy POST /customer-trips soft-deprecated (unsafe Ride side channel).
    /// </summary>
    public const string CustomerTripsDeprecated = "CUSTOMER_TRIPS_DEPRECATED";

    // Phase 6C — Ride + Group
    public const string RideFareNotConfigured = "RIDE_FARE_NOT_CONFIGURED";
    public const string GroupFareNotConfigured = "GROUP_FARE_NOT_CONFIGURED";
    public const string RideNotFound = "RIDE_NOT_FOUND";
    public const string RideNotCancellable = "RIDE_NOT_CANCELLABLE";
    public const string RideNotAssignable = "RIDE_NOT_ASSIGNABLE";
    public const string RideNotAssigned = "RIDE_NOT_ASSIGNED";
    public const string RideNotStartable = "RIDE_NOT_STARTABLE";
    public const string RideNotCompletable = "RIDE_NOT_COMPLETABLE";
    public const string RideAlreadyStarted = "RIDE_ALREADY_STARTED";
    public const string RideAlreadyCompleted = "RIDE_ALREADY_COMPLETED";
    public const string RideCancelled = "RIDE_CANCELLED";
    public const string RideCashRequired = "RIDE_CASH_REQUIRED";
    public const string GroupNotFound = "GROUP_NOT_FOUND";
    public const string GroupNotCancellable = "GROUP_NOT_CANCELLABLE";
    public const string GroupNotAssignable = "GROUP_NOT_ASSIGNABLE";
    public const string GroupNotAssigned = "GROUP_NOT_ASSIGNED";
    public const string GroupNotStartable = "GROUP_NOT_STARTABLE";
    public const string GroupNotCompletable = "GROUP_NOT_COMPLETABLE";
    public const string GroupMembershipLocked = "GROUP_MEMBERSHIP_LOCKED";
    public const string GroupCapacityExceeded = "GROUP_CAPACITY_EXCEEDED";
    public const string GroupDuplicateMember = "GROUP_DUPLICATE_MEMBER";
    public const string GroupNotConfirmable = "GROUP_NOT_CONFIRMABLE";
    public const string GroupInvalidCapacity = "GROUP_INVALID_CAPACITY";
    public const string GroupNotMember = "GROUP_NOT_MEMBER";
    public const string DriverRideConflict = "DRIVER_RIDE_CONFLICT";
    public const string DriverGroupConflict = "DRIVER_GROUP_CONFLICT";
    public const string RideLocationNotAllowed = "RIDE_LOCATION_NOT_ALLOWED";
    public const string RideLocationInvalid = "RIDE_LOCATION_INVALID";

    // Phase 6M — distance pricing
    public const string TripDistanceNotCalculated = "TRIP_DISTANCE_NOT_CALCULATED";
    public const string TripCoordinatesRequired = "TRIP_COORDINATES_REQUIRED";
}
