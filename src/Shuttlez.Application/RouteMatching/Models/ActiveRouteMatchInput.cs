namespace Shuttlez.Application.RouteMatching.Models;

public sealed record ActiveRouteMatchInput(
    Guid RouteId,
    string EncodedPolyline,
    DateTime? NextDepartureTime,
    double? BoundsMinLatitude,
    double? BoundsMaxLatitude,
    double? BoundsMinLongitude,
    double? BoundsMaxLongitude);
