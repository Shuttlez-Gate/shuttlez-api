using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.RouteMatching.Models;
using Shuttlez.Domain.Entities;

namespace Shuttlez.Application.Admin.Services;

public interface IRoutePolylineService
{
    /// <summary>
    /// يولّد polyline حقيقي للخط (بداية → محطات → نهاية) ويحدّث الكيان دون حفظ.
    /// يرجع true لو تم تحديث الكيان.
    /// </summary>
    Task<bool> RegenerateAsync(Route route, CancellationToken ct = default);
}

public sealed class RoutePolylineService : IRoutePolylineService
{
    private readonly IAppDbContext _db;
    private readonly IGoogleDirectionsService _directions;

    public RoutePolylineService(IAppDbContext db, IGoogleDirectionsService directions)
    {
        _db = db;
        _directions = directions;
    }

    public async Task<bool> RegenerateAsync(Route route, CancellationToken ct = default)
    {
        var origin = new GeoCoordinate(route.StartLatitude, route.StartLongitude);
        var destination = new GeoCoordinate(route.EndLatitude, route.EndLongitude);

        var waypoints = await _db.Stops
            .Where(s => s.RouteId == route.Id)
            .OrderBy(s => s.Order)
            .Select(s => new GeoCoordinate(s.Latitude, s.Longitude))
            .ToListAsync(ct);

        var directions = await _directions.GetDirectionsAsync(
            origin,
            destination,
            waypoints,
            ct);

        if (directions is null || string.IsNullOrWhiteSpace(directions.EncodedPolyline))
        {
            return false;
        }

        route.EncodedPolyline = directions.EncodedPolyline;
        route.DistanceMeters = directions.DistanceMeters > 0 ? directions.DistanceMeters : route.DistanceMeters;
        route.DurationSeconds = directions.DurationSeconds > 0 ? directions.DurationSeconds : route.DurationSeconds;
        return true;
    }
}
