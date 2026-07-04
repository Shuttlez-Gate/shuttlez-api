namespace Shuttlez.Application.Routes.DTOs;

public record RouteListItemDto(
    Guid Id,
    string From,
    string To,
    int StopsCount,
    string NearestStop,
    string NearestStreet,
    string BusPlate,
    int BadgeColorStart,
    int BadgeColorEnd);

public record RouteTimelineStopDto(
    string Kind,
    string Title,
    string Subtitle,
    string? Badge);

public record RouteTimelineDto(
    Guid RouteId,
    IReadOnlyList<RouteTimelineStopDto> Stops);
