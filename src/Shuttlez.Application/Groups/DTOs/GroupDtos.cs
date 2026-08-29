namespace Shuttlez.Application.Groups.DTOs;

public record CreateGroupRequest(
    double PickupLatitude,
    double PickupLongitude,
    double DestinationLatitude,
    double DestinationLongitude,
    int Capacity,
    string? PickupAddress = null,
    string? DestinationAddress = null,
    string? FromZoneKey = null,
    string? ToZoneKey = null);

public record ConfirmGroupCashRequest(string? PaymentMethod = null);

public record GroupQuoteDto(
    Guid GroupFareRuleId,
    string RuleName,
    string? FromZoneKey,
    string? ToZoneKey,
    decimal CharterFlatFare,
    int MaxMembers,
    decimal PlatformCommissionPercent,
    decimal CommissionAmount,
    decimal CaptainEarnings,
    decimal TotalAmount,
    string PaymentMethodHint,
    decimal? DistanceKm = null,
    decimal? BaseFare = null,
    decimal? PricePerKm = null,
    decimal? MinimumFare = null);

public record GroupFareOptionDto(
    Guid Id,
    string Name,
    string? FromZoneKey,
    string? ToZoneKey,
    decimal CharterFlatFare,
    int MaxMembers);

public record GroupMemberDto(
    Guid UserId,
    string? DisplayName,
    bool IsOrganizer,
    DateTime JoinedAt);

public record GroupDto(
    Guid Id,
    Guid OrganizerUserId,
    double PickupLatitude,
    double PickupLongitude,
    string? PickupAddress,
    double DestinationLatitude,
    double DestinationLongitude,
    string? DestinationAddress,
    string? FromZoneKey,
    string? ToZoneKey,
    Guid? GroupFareRuleId,
    int Capacity,
    int JoinedMemberCount,
    string Status,
    decimal FareAmount,
    decimal CommissionRate,
    decimal CommissionAmount,
    decimal CaptainEarnings,
    decimal TotalAmount,
    string PaymentMethod,
    bool IsCashConfirmed,
    bool MembershipLocked,
    Guid? DriverId,
    string? DriverName,
    DateTime? ConfirmedAt,
    DateTime? AssignedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    DateTime? CancelledAt,
    string? ReferenceCode,
    DateTime CreatedAt,
    IReadOnlyList<GroupMemberDto> Members,
    decimal? DistanceKm = null);

public record DriverGroupLifecycleDto(
    Guid GroupId,
    Guid? DriverId,
    string Status,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    DateTime? UpdatedAt,
    string Message,
    bool Idempotent = false);

public record AssignGroupDriverRequest(Guid DriverId);
