namespace Shuttlez.Domain.Enums;

/// Private/charter Group product lifecycle (Phase 6C). Not RouteDemandGroupState.
public enum GroupRequestStatus
{
    Draft = 1,
    Confirmed = 2,
    Assigned = 3,
    InProgress = 4,
    Completed = 5,
    Cancelled = 6
}
