using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.CustomerTrips.DTOs;
using Shuttlez.Application.RouteMatching;
using Shuttlez.Application.RouteMatching.Models;
using Shuttlez.Domain.Entities;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.CustomerTrips.Commands;

public record CreateCustomerTripCommand(CreateCustomerTripRequest Request)
    : IRequest<CreateCustomerTripResponse>;

public class CreateCustomerTripHandler : IRequestHandler<CreateCustomerTripCommand, CreateCustomerTripResponse>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IRouteMatchingService _routeMatching;
    private readonly IGoogleDirectionsService _directions;
    private readonly IDateTimeProvider _clock;
    private readonly RouteMatchingOptions _matchingOptions;

    public CreateCustomerTripHandler(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IRouteMatchingService routeMatching,
        IGoogleDirectionsService directions,
        IDateTimeProvider clock,
        IOptions<RouteMatchingOptions> matchingOptions)
    {
        _db = db;
        _currentUser = currentUser;
        _routeMatching = routeMatching;
        _directions = directions;
        _clock = clock;
        _matchingOptions = matchingOptions.Value;
    }

    public async Task<CreateCustomerTripResponse> Handle(
        CreateCustomerTripCommand request,
        CancellationToken cancellationToken)
    {
        _ = _currentUser.UserId ?? throw new UnauthorizedAppException("غير مصرح");

        var form = request.Request;
        ValidateCoordinates(
            form.OriginLatitude,
            form.OriginLongitude,
            form.DestinationLatitude,
            form.DestinationLongitude);

        var origin = new GeoCoordinate(form.OriginLatitude, form.OriginLongitude);
        var destination = new GeoCoordinate(form.DestinationLatitude, form.DestinationLongitude);
        var now = _clock.UtcNow;

        var activeRoutes = await LoadActiveRoutesForMatching(origin, destination, now, cancellationToken);
        var matches = _routeMatching.MatchUserWithExistingRoutes(origin, destination, activeRoutes);

        if (matches.Count > 0)
        {
            var best = matches[0];
            return new CreateCustomerTripResponse(
                true,
                best.RouteId,
                null,
                MapMatch(best),
                "تم العثور على مسار متوافق");
        }

        var directions = await _directions.GetDirectionsAsync(origin, destination, cancellationToken)
            ?? throw new AppException("تعذر حساب مسار الرحلة");

        var decoded = _routeMatching.DecodePolyline(directions.EncodedPolyline);
        var bounds = _routeMatching.ComputeBounds(decoded);

        var routeName = BuildRouteName(form.OriginAddress, form.DestinationAddress);
        var route = new Route
        {
            Name = routeName,
            Description = routeName,
            StartLatitude = origin.Latitude,
            StartLongitude = origin.Longitude,
            EndLatitude = destination.Latitude,
            EndLongitude = destination.Longitude,
            EncodedPolyline = directions.EncodedPolyline,
            DistanceMeters = directions.DistanceMeters,
            DurationSeconds = directions.DurationSeconds,
            BoundsMinLatitude = bounds.MinLatitude,
            BoundsMaxLatitude = bounds.MaxLatitude,
            BoundsMinLongitude = bounds.MinLongitude,
            BoundsMaxLongitude = bounds.MaxLongitude,
            IsActive = true,
        };

        var scheduledAt = form.PreferredDepartureTime ?? now.Date.AddDays(1).AddHours(7);
        if (scheduledAt <= now)
            scheduledAt = now.AddHours(2);

        var trip = new Trip
        {
            Route = route,
            Status = TripStatus.Scheduled,
            ScheduledAt = scheduledAt,
            PricePerSeat = 120,
            AvailableSeats = 14,
            ReferenceCode = $"TR-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
        };

        _db.Add(route);
        _db.Add(trip);
        await _db.SaveChangesAsync(cancellationToken);

        return new CreateCustomerTripResponse(
            false,
            route.Id,
            trip.Id,
            null,
            "تم إنشاء مسار جديد");
    }

    private async Task<List<ActiveRouteMatchInput>> LoadActiveRoutesForMatching(
        GeoCoordinate origin,
        GeoCoordinate destination,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var bufferDegrees = _matchingOptions.MaxDistanceMeters / 111_320d;
        var corridorMinLat = Math.Min(origin.Latitude, destination.Latitude) - bufferDegrees;
        var corridorMaxLat = Math.Max(origin.Latitude, destination.Latitude) + bufferDegrees;
        var corridorMinLng = Math.Min(origin.Longitude, destination.Longitude) - bufferDegrees;
        var corridorMaxLng = Math.Max(origin.Longitude, destination.Longitude) + bufferDegrees;

        var routes = await _db.Routes
            .AsNoTracking()
            .Where(r =>
                r.IsActive &&
                !r.IsDeleted &&
                r.EncodedPolyline != null &&
                (r.BoundsMinLatitude == null ||
                 (r.BoundsMaxLatitude >= corridorMinLat &&
                  r.BoundsMinLatitude <= corridorMaxLat &&
                  r.BoundsMaxLongitude >= corridorMinLng &&
                  r.BoundsMinLongitude <= corridorMaxLng)))
            .Select(r => new
            {
                r.Id,
                r.EncodedPolyline,
                r.BoundsMinLatitude,
                r.BoundsMaxLatitude,
                r.BoundsMinLongitude,
                r.BoundsMaxLongitude,
                NextDepartureTime = r.Trips
                    .Where(t =>
                        !t.IsDeleted &&
                        (t.Status == TripStatus.Scheduled || t.Status == TripStatus.DriverAssigned) &&
                        t.ScheduledAt >= now &&
                        t.AvailableSeats > 0)
                    .OrderBy(t => t.ScheduledAt)
                    .Select(t => (DateTime?)t.ScheduledAt)
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        return routes
            .Where(r => !string.IsNullOrWhiteSpace(r.EncodedPolyline))
            .Select(r => new ActiveRouteMatchInput(
                r.Id,
                r.EncodedPolyline!,
                r.NextDepartureTime,
                r.BoundsMinLatitude,
                r.BoundsMaxLatitude,
                r.BoundsMinLongitude,
                r.BoundsMaxLongitude))
            .ToList();
    }

    private static void ValidateCoordinates(
        double originLat,
        double originLng,
        double destinationLat,
        double destinationLng)
    {
        if (!IsValidLatitude(originLat) || !IsValidLatitude(destinationLat))
            throw new AppException("إحداثيات خط العرض غير صالحة");

        if (!IsValidLongitude(originLng) || !IsValidLongitude(destinationLng))
            throw new AppException("إحداثيات خط الطول غير صالحة");

        if (Math.Abs(originLat - destinationLat) < 1e-6 &&
            Math.Abs(originLng - destinationLng) < 1e-6)
        {
            throw new AppException("نقطة البداية والنهاية متطابقتان");
        }
    }

    private static bool IsValidLatitude(double value) => value is >= -90 and <= 90;
    private static bool IsValidLongitude(double value) => value is >= -180 and <= 180;

    private static string BuildRouteName(string? originAddress, string? destinationAddress)
    {
        var origin = string.IsNullOrWhiteSpace(originAddress) ? "نقطة البداية" : originAddress.Trim();
        var destination = string.IsNullOrWhiteSpace(destinationAddress) ? "نقطة النهاية" : destinationAddress.Trim();
        return $"{origin} → {destination}";
    }

    private static RouteMatchResultDto MapMatch(RouteMatchResult match) =>
        new(
            match.RouteId,
            match.MatchPercentage,
            match.OriginDistanceMeters,
            match.DestinationDistanceMeters,
            ToDto(match.OriginClosestPoint),
            ToDto(match.DestinationClosestPoint),
            ToDto(match.SuggestedPickupPoint),
            ToDto(match.SuggestedDropoffPoint),
            match.NextDepartureTime,
            match.DeviationMeters,
            match.TotalDistanceMeters);

    private static GeoCoordinateDto ToDto(GeoCoordinate coordinate) =>
        new(coordinate.Latitude, coordinate.Longitude);
}
