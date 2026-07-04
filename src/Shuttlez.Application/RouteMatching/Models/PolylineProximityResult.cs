namespace Shuttlez.Application.RouteMatching.Models;

public sealed record PolylineProximityResult(
    double DistanceMeters,
    GeoCoordinate ClosestPoint,
    int SegmentIndex,
    double DistanceAlongPolylineMeters);
