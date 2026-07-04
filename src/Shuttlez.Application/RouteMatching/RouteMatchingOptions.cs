namespace Shuttlez.Application.RouteMatching;

public class RouteMatchingOptions
{
    public const string SectionName = "RouteMatching";

    /// <summary>Maximum distance (meters) from a point to the route polyline to count as a match.</summary>
    public double MaxDistanceMeters { get; set; } = 300;

    /// <summary>When a polyline has more points than this, it is down-sampled for matching.</summary>
    public int MaxPolylinePointsForMatching { get; set; } = 500;
}
