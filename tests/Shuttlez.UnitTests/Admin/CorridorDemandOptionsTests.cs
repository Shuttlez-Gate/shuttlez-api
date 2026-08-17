using Microsoft.Extensions.Options;
using Shuttlez.Application.RouteMatching;
using Shuttlez.Application.RouteMatching.Models;

namespace Shuttlez.UnitTests.Admin;

public class CorridorDemandOptionsTests
{
    [Fact]
    public void CorridorDemandMeters_DefaultsTo100()
    {
        var options = new RouteMatchingOptions();
        Assert.Equal(100, options.CorridorDemandMeters);
    }

    [Fact]
    public void PointWithin100MetersOfPolyline_IsNearRoute()
    {
        var matching = new RouteMatchingService(Options.Create(new RouteMatchingOptions
        {
            MaxDistanceMeters = 300,
            CorridorDemandMeters = 100,
            MaxPolylinePointsForMatching = 500,
        }));

        // ~111m per 0.001° latitude near equator; 0.0005° ≈ 55m
        var polyline = new[]
        {
            new GeoCoordinate(30.0000, 31.0000),
            new GeoCoordinate(30.0100, 31.0000),
        };
        var nearPoint = new GeoCoordinate(30.0050, 31.0004);

        Assert.True(matching.IsPointNearRoute(nearPoint, polyline, 100));

        var farPoint = new GeoCoordinate(30.0050, 31.0050); // ~500m+ east
        Assert.False(matching.IsPointNearRoute(farPoint, polyline, 100));
    }
}
