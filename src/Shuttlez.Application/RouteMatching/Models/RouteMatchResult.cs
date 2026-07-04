namespace Shuttlez.Application.RouteMatching.Models;

public sealed record RouteMatchResult(
    Guid RouteId,
    double MatchPercentage,
    double OriginDistanceMeters,
    double DestinationDistanceMeters,
    GeoCoordinate OriginClosestPoint,
    GeoCoordinate DestinationClosestPoint,
    GeoCoordinate SuggestedPickupPoint,
    GeoCoordinate SuggestedDropoffPoint,
    DateTime? NextDepartureTime,
    double DeviationMeters,
    double TotalDistanceMeters);
