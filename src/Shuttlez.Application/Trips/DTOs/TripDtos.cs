namespace Shuttlez.Application.Trips.DTOs;

public record TripListItemDto(
    Guid Id,
    string ReferenceCode,
    string DateTimeLabel,
    DateTime TripDate,
    string VehicleType,
    string From,
    string To,
    string Status,
    string VehicleAssetKey,
    double StartLatitude,
    double StartLongitude,
    double EndLatitude,
    double EndLongitude,
    string? ArrivalTime,
    string? ArrivalStreet,
    double? Progress);

public record TripListResponse(
    TripListItemDto? CurrentTrip,
    IReadOnlyList<TripListItemDto> UpcomingTrips,
    IReadOnlyList<TripListItemDto> HistoryTrips);

public record TripRoutePointDto(
    string Address,
    string Time,
    int DotColorArgb);

public record TripTimelineStopDto(
    string Kind,
    string Title,
    string Subtitle,
    string? Badge,
    string? LeftTime);

public record TripDetailsDto(
    TripListItemDto Trip,
    string VehicleDescription,
    string DriverName,
    string DriverPhotoUrl,
    double DriverRating,
    string? DriverRatingSubtitle,
    string SeatsLabel,
    string PriceLabel,
    string DepartureMapTime,
    string ArrivalMapTime,
    TripRoutePointDto Pickup,
    TripRoutePointDto Dropoff,
    IReadOnlyList<TripTimelineStopDto> Stops,
    string? PickupAddress,
    string? DropoffAddress,
    string? EtaMinutes,
    string? EtaDistance,
    string? VehicleTitle,
    string? VehiclePlate,
    int UnreadMessages);

public record InvoiceLineItemDto(
    string Title,
    string Value,
    bool IsTotal);

public record TripInvoiceDto(
    string TripId,
    string DateTimeLabel,
    string PassengerName,
    IReadOnlyList<InvoiceLineItemDto> LineItems);
