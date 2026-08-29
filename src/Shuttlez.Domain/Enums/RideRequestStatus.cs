namespace Shuttlez.Domain.Enums;

/// Private-car Ride product lifecycle (Phase 6C). Not Shuttle TripStatus.
public enum RideRequestStatus
{
    Requested = 1,
    Assigned = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5
}
