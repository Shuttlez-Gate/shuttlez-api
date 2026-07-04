using Shuttlez.Application.RouteMatching.Models;

namespace Shuttlez.Application.RouteMatching;

public interface IRouteMatchingService
{
    IReadOnlyList<GeoCoordinate> DecodePolyline(string encodedPolyline);

    PolylineProximityResult DistanceFromPointToPolyline(
        GeoCoordinate point,
        IReadOnlyList<GeoCoordinate> polyline);

    bool IsPointNearRoute(
        GeoCoordinate point,
        IReadOnlyList<GeoCoordinate> polyline,
        double maxDistanceMeters);

    IReadOnlyList<RouteMatchResult> MatchUserWithExistingRoutes(
        GeoCoordinate origin,
        GeoCoordinate destination,
        IEnumerable<ActiveRouteMatchInput> activeRoutes);

    (double MinLatitude, double MaxLatitude, double MinLongitude, double MaxLongitude) ComputeBounds(
        IReadOnlyList<GeoCoordinate> polyline);
}
