using Shuttlez.Domain.Common;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Domain.Entities;

/// Private/charter Group booking. Not RouteDemandGroupState.
public class GroupRequest : BaseEntity
{
    public Guid OrganizerUserId { get; set; }

    public double PickupLatitude { get; set; }
    public double PickupLongitude { get; set; }
    public string? PickupAddress { get; set; }

    public double DestinationLatitude { get; set; }
    public double DestinationLongitude { get; set; }
    public string? DestinationAddress { get; set; }

    public string? FromZoneKey { get; set; }
    public string? ToZoneKey { get; set; }
    public Guid? GroupFareRuleId { get; set; }

    /// Capacity chosen by organizer at create; capped by fare rule MaxMembers.
    public int Capacity { get; set; }

    /// Denormalized joined count for atomic capacity checks.
    public int JoinedMemberCount { get; set; }

    public GroupRequestStatus Status { get; set; } = GroupRequestStatus.Draft;

    public decimal? DistanceKm { get; set; }
    public decimal? BaseFareApplied { get; set; }
    public decimal? PricePerKmApplied { get; set; }
    public decimal? MinimumFareApplied { get; set; }

    public decimal FareAmount { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal CaptainEarnings { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = "cash";
    public bool IsCashConfirmed { get; set; }
    public bool MembershipLocked { get; set; }

    public Guid? DriverId { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public string? ReferenceCode { get; set; }

    public User Organizer { get; set; } = null!;
    public Driver? Driver { get; set; }
    public GroupFareRule? GroupFareRule { get; set; }
    public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
}
