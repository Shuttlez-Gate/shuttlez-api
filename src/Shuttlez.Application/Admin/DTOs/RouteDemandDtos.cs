namespace Shuttlez.Application.Admin.DTOs;

public record RouteDemandSummaryDto(
    int TotalRouteRequests,
    int UniquePassengers,
    int UniqueRoutes,
    string? TopRouteLabel,
    int TopRouteDemand);

public record RouteDemandPassengerDto(
    Guid Id,
    string Source,
    string? PassengerName,
    string Phone,
    string From,
    string To,
    string? WorkOrUniversity,
    string? PreferredDepartureTime,
    string? PreferredReturnTime,
    string? Days,
    string LeadStatus,
    bool IsConfirmed,
    DateTime CreatedAt);

public record RouteDemandTimeBucketDto(string Time, int PassengerCount);

public record RouteDemandDayBucketDto(string Day, int PassengerCount);

public record RouteDemandCaptainDto(
    Guid DriverId,
    string Name,
    string Phone,
    string VehicleType,
    int Capacity,
    string Status);

public record RouteDemandRowDto(
    int Rank,
    string RouteKey,
    string RouteLabel,
    string EndpointA,
    string EndpointB,
    int TotalRequests,
    int ConfirmedPassengers,
    int UniquePassengers,
    string RecommendedVehicle,
    int VehicleCapacity,
    int RemainingSeats,
    bool CapacityExceeded,
    string Priority,
    string Status,
    string RouteType,
    DateTime FirstRequestAt,
    DateTime LastRequestAt,
    Guid? AssignedDriverId,
    string? AssignedDriverName,
    /// <summary>Pricing / launch readiness (estimated; not bookings).</summary>
    RouteDemandReadinessDto? Readiness = null);

public record RouteDemandDetailsDto(
    string RouteKey,
    string RouteLabel,
    string EndpointA,
    string EndpointB,
    int TotalRequests,
    int ConfirmedPassengers,
    int UniquePassengers,
    string RecommendedVehicle,
    int VehicleCapacity,
    int RemainingSeats,
    double CapacityPercent,
    bool CapacityExceeded,
    string Priority,
    string Status,
    string RouteType,
    string LaunchRecommendation,
    string LaunchReason,
    string NextAction,
    RouteDemandCaptainDto? Captain,
    IReadOnlyList<RouteDemandPassengerDto> Passengers,
    IReadOnlyList<RouteDemandTimeBucketDto> PreferredDepartureTimes,
    IReadOnlyList<RouteDemandDayBucketDto> WorkDays,
    RouteDemandReadinessDto? Readiness = null);

/// <summary>
/// Admin launch-readiness snapshot for a demand corridor.
/// Financial preview is at configured minimumLaunchRiders only — not collected revenue.
/// </summary>
public record RouteDemandReadinessDto(
    Guid? RouteId,
    string? RouteName,
    string? VehicleType,
    string? VehicleTypeName,
    int? Capacity,
    int DemandCount,
    int UniquePassengers,
    int ConfirmedPassengers,
    decimal? OccupancyPercent,
    int? MinimumLaunchRiders,
    int? TargetOccupancy,
    int? RidersRequired,
    int? RemainingToTarget,
    bool PricingAvailable,
    bool PricingLinked,
    string? PricingSource,
    decimal? OneWayPrice,
    decimal? RoundTripPrice,
    decimal? WeeklyPrice,
    decimal? MonthlyPrice,
    decimal? CommissionRate,
    string? CommissionType,
    int? LaunchPeriodDays,
    DateTime? LaunchStartAt,
    DateTime? LaunchEndAt,
    bool? LaunchActive,
    decimal? FinancialAtMinimumOneWayGross,
    decimal? FinancialAtMinimumRoundTripGross,
    decimal? FinancialAtMinimumPlatformCommission,
    decimal? FinancialAtMinimumCaptainEarnings,
    string LaunchStatus,
    string ReasonCode,
    string ReadinessReason,
    Guid? PricingRuleId,
    DateTime? LastUpdatedAt,
    /// <summary>Heuristic capacity from demand-band recommendation (NOT source of truth).</summary>
    int? DemandBandCapacity = null,
    /// <summary>VEHICLE_MASTER or DEFAULT_HINT for authoritative Capacity.</summary>
    string? CapacitySource = null,
    /// <summary>True when demand-band capacity differs from authoritative Capacity.</summary>
    bool HasCapacityConflict = false,
    /// <summary>EXPLICIT (Admin) or EXACT_KEY. Null when unlinked.</summary>
    string? RouteLinkSource = null,
    /// <summary>
    /// When RouteLinkSource is EXPLICIT: EXACT_BIDIRECTIONAL if demand key matches Route.Name endpoints;
    /// otherwise MANUAL_OVERRIDE (Admin chose a non-exact corridor — never fuzzy).
    /// </summary>
    string? MappingCompatibility = null);

public record RouteDemandAnalysisQuery(
    string? Search = null,
    string? From = null,
    string? To = null,
    string? VehicleType = null,
    string? Priority = null,
    string? Status = null,
    string? RouteType = null,
    string? RouteCategory = null,
    DateTime? CreatedFrom = null,
    DateTime? CreatedTo = null,
    int? Page = null,
    int? PageSize = null,
    string? LaunchStatus = null,
    bool? PricingAvailable = null,
    bool? ReadyToLaunch = null,
    string? RouteKey = null);

public record UpdateRouteDemandStatusRequest(string Status, Guid? AssignedDriverId = null);

public record MapRouteDemandRequest(Guid RouteId);

/// <summary>Admin launch parameters only — price/capacity/commission resolved server-side.</summary>
public record LaunchRouteDemandRequest(
    DateTime ScheduledAt,
    Guid? DriverId = null,
    Guid? VehicleId = null);

public record RouteDemandLaunchResultDto(
    Guid TripId,
    Guid RouteId,
    string RouteName,
    Guid? DriverId,
    Guid? VehicleId,
    string? VehicleType,
    string Status,
    DateTime ScheduledAt,
    decimal PricePerSeat,
    int AvailableSeats,
    decimal CommissionPercent,
    string? ReferenceCode,
    DateTime CreatedAt,
    string Message);

public record RouteDemandExportRowDto(
    int Rank,
    string Route,
    string From,
    string To,
    int Demand,
    int UniquePassengers,
    int ConfirmedPassengers,
    string RecommendedVehicle,
    int Capacity,
    int RemainingCapacity,
    string Priority,
    string Status,
    string? PreferredTime,
    string? Captain,
    DateTime CreatedDate);
