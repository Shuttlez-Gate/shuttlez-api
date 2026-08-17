namespace Shuttlez.Application.Admin.DTOs;

public record CorridorMatchedRequestDto(
    Guid Id,
    Guid UserId,
    string UserPhone,
    string? UserName,
    string FromAddress,
    string ToAddress,
    string PreferredVehicleType,
    int WeeklyCount,
    int Seats,
    double FromDistanceMeters,
    double ToDistanceMeters,
    string Status);

public record FleetAvailabilityDto(
    string VehicleType,
    int VehicleCount,
    int TotalCapacity,
    IReadOnlyList<FleetVehicleItemDto> Vehicles);

public record FleetVehicleItemDto(
    Guid VehicleId,
    string PlateNumber,
    string Model,
    int Capacity,
    Guid? DriverId,
    string? DriverName);

public record SeatAssignmentDto(
    string PreferredVehicleType,
    int SeatsNeeded,
    int SeatsCovered,
    int SeatsShortfall,
    string AssignedVehicleType,
    int VehiclesRequired,
    int CapacityPerVehicle,
    IReadOnlyList<Guid> VehicleIds,
    string? Note);

public record ProposedTripDto(
    string VehicleType,
    Guid? VehicleId,
    string? VehiclePlate,
    Guid? DriverId,
    string? DriverName,
    int AvailableSeats,
    decimal SuggestedPricePerSeat);

public record CorridorDemandReportDto(
    Guid RouteId,
    string RouteName,
    double CorridorMeters,
    bool HasPolyline,
    int MatchedRequestsCount,
    int TotalSeatsNeeded,
    IReadOnlyDictionary<string, int> SeatsNeededByVehicleType,
    IReadOnlyList<FleetAvailabilityDto> AvailableFleet,
    IReadOnlyList<SeatAssignmentDto> ProposedAssignment,
    IReadOnlyList<ProposedTripDto> ProposedTrips,
    IReadOnlyList<CorridorMatchedRequestDto> MatchedRequests);

public record ApplyCorridorDemandRequest(
    DateTime? ScheduledAt = null,
    decimal? PricePerSeat = null);

public record ApplyCorridorDemandResultDto(
    int TripsCreated,
    int RequestsConverted,
    IReadOnlyList<Guid> TripIds);
