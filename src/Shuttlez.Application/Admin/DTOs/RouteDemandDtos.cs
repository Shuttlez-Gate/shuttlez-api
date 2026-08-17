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
    string? AssignedDriverName);

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
    IReadOnlyList<RouteDemandDayBucketDto> WorkDays);

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
    int? PageSize = null);

public record UpdateRouteDemandStatusRequest(string Status, Guid? AssignedDriverId = null);

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
