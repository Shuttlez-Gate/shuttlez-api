using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.RouteMatching;
using Shuttlez.Application.RouteMatching.Models;
using Shuttlez.Application.RouteRequests;
using Shuttlez.Domain.Entities;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Admin.Services;

public interface ICorridorDemandService
{
    Task<CorridorDemandReportDto> AnalyzeAsync(Guid routeId, CancellationToken ct = default);

    Task<ApplyCorridorDemandResultDto> ApplyAsync(
        Guid routeId,
        ApplyCorridorDemandRequest request,
        CancellationToken ct = default);
}

public sealed class CorridorDemandService : ICorridorDemandService
{
    private static readonly string[] MatchableStatuses = ["pending", "approved"];

    private readonly IAppDbContext _db;
    private readonly IRouteMatchingService _matching;
    private readonly IDateTimeProvider _clock;
    private readonly IRoutePolylineService _routePolyline;
    private readonly RouteMatchingOptions _options;

    public CorridorDemandService(
        IAppDbContext db,
        IRouteMatchingService matching,
        IDateTimeProvider clock,
        IRoutePolylineService routePolyline,
        IOptions<RouteMatchingOptions> options)
    {
        _db = db;
        _matching = matching;
        _clock = clock;
        _routePolyline = routePolyline;
        _options = options.Value;
    }

    public async Task<CorridorDemandReportDto> AnalyzeAsync(
        Guid routeId,
        CancellationToken ct = default)
    {
        var route = await _db.Routes
            .FirstOrDefaultAsync(r => r.Id == routeId && !r.IsDeleted, ct)
            ?? throw new NotFoundException("الخط غير موجود");

        var corridorMeters = _options.CorridorDemandMeters <= 0
            ? 100
            : _options.CorridorDemandMeters;

        var polyline = await BuildPolylineAsync(route, ct);
        var hasPolyline = polyline.Count >= 2;

        var requests = await _db.RouteRequests
            .Where(r => !r.IsDeleted && MatchableStatuses.Contains(r.Status))
            .Select(r => new
            {
                r.Id,
                r.UserId,
                Phone = r.User.Phone,
                Name = r.User.FullName,
                r.FromAddress,
                r.ToAddress,
                r.FromLatitude,
                r.FromLongitude,
                r.ToLatitude,
                r.ToLongitude,
                r.PreferredVehicleType,
                r.Status,
                r.Notes
            })
            .ToListAsync(ct);

        var matched = new List<CorridorMatchedRequestDto>();

        foreach (var req in requests)
        {
            if (!EgyptAreaCoordinates.HasValidCoordinates(req.FromLatitude, req.FromLongitude) ||
                !EgyptAreaCoordinates.HasValidCoordinates(req.ToLatitude, req.ToLongitude))
            {
                continue;
            }

            var origin = new GeoCoordinate(req.FromLatitude, req.FromLongitude);
            var destination = new GeoCoordinate(req.ToLatitude, req.ToLongitude);

            double fromDist;
            double toDist;
            bool directionOk;

            if (hasPolyline)
            {
                var fromProx = _matching.DistanceFromPointToPolyline(origin, polyline);
                var toProx = _matching.DistanceFromPointToPolyline(destination, polyline);
                fromDist = fromProx.DistanceMeters;
                toDist = toProx.DistanceMeters;
                directionOk = fromProx.DistanceAlongPolylineMeters <= toProx.DistanceAlongPolylineMeters;
            }
            else
            {
                // بدون polyline: قرب من نقطتي البداية/النهاية للخط.
                fromDist = Haversine(
                    origin.Latitude, origin.Longitude,
                    route.StartLatitude, route.StartLongitude);
                toDist = Haversine(
                    destination.Latitude, destination.Longitude,
                    route.EndLatitude, route.EndLongitude);
                directionOk = true;
            }

            if (fromDist > corridorMeters || toDist > corridorMeters || !directionOk)
            {
                continue;
            }

            var weekly = ParseWeeklyCount(req.Notes);
            var vehicle = NormalizeVehicle(req.PreferredVehicleType);
            matched.Add(new CorridorMatchedRequestDto(
                req.Id,
                req.UserId,
                req.Phone,
                req.Name,
                req.FromAddress,
                req.ToAddress,
                vehicle,
                weekly,
                Seats: 1,
                fromDist,
                toDist,
                req.Status));
        }

        var seatsByType = matched
            .GroupBy(m => m.PreferredVehicleType)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Seats), StringComparer.OrdinalIgnoreCase);

        var fleet = await LoadFleetAsync(ct);
        var assignment = SeatDemandDistributor.Distribute(seatsByType, fleet);
        var proposedTrips = SeatDemandDistributor.BuildProposedTrips(assignment, fleet);

        return new CorridorDemandReportDto(
            route.Id,
            route.Name,
            corridorMeters,
            hasPolyline,
            matched.Count,
            matched.Sum(m => m.Seats),
            seatsByType,
            fleet,
            assignment,
            proposedTrips,
            matched.OrderBy(m => m.FromDistanceMeters).ToList());
    }

    public async Task<ApplyCorridorDemandResultDto> ApplyAsync(
        Guid routeId,
        ApplyCorridorDemandRequest request,
        CancellationToken ct = default)
    {
        var report = await AnalyzeAsync(routeId, ct);
        if (report.ProposedTrips.Count == 0)
        {
            throw new AppException(
                report.MatchedRequestsCount == 0
                    ? "لا توجد طلبات مطابقة على مسار هذا الخط (±100م)"
                    : "لا توجد مركبات متاحة لتغطية الطلب");
        }

        var scheduledAt = request.ScheduledAt.HasValue
            ? ToUtc(request.ScheduledAt.Value)
            : _clock.UtcNow.Date.AddDays(1).AddHours(7);

        if (scheduledAt <= _clock.UtcNow)
        {
            scheduledAt = _clock.UtcNow.AddHours(3);
        }

        var price = request.PricePerSeat is null or <= 0
            ? 100m
            : request.PricePerSeat.Value;

        var tripIds = new List<Guid>();
        foreach (var proposal in report.ProposedTrips)
        {
            var trip = new Trip
            {
                RouteId = routeId,
                DriverId = proposal.DriverId,
                Status = proposal.DriverId is null
                    ? TripStatus.Scheduled
                    : TripStatus.DriverAssigned,
                ScheduledAt = scheduledAt,
                PricePerSeat = price,
                AvailableSeats = proposal.AvailableSeats,
                ReferenceCode = "TRP-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()
            };
            _db.Add(trip);
            tripIds.Add(trip.Id);
        }

        var requestIds = report.MatchedRequests.Select(r => r.Id).ToList();
        var entities = await _db.RouteRequests
            .Where(r => requestIds.Contains(r.Id))
            .ToListAsync(ct);

        foreach (var entity in entities)
        {
            entity.Status = "converted";
            entity.UpdatedAt = _clock.UtcNow;
            _db.Update(entity);

            _db.Add(new Notification
            {
                UserId = entity.UserId,
                Title = "خط السير أصبح متاحاً",
                Body = $"تم تشغيل خط قريب من طلبك ({entity.FromAddress} ← {entity.ToAddress}) ويمكن الحجز عليه.",
                Type = "route_request"
            });
        }

        await _db.SaveChangesAsync(ct);

        return new ApplyCorridorDemandResultDto(
            tripIds.Count,
            entities.Count,
            tripIds);
    }

    private async Task<IReadOnlyList<GeoCoordinate>> BuildPolylineAsync(
        Route route,
        CancellationToken ct)
    {
        // خطوط قديمة بدون polyline: ولّده مرة واحدة (بداية → محطات → نهاية) واحفظه.
        if (string.IsNullOrWhiteSpace(route.EncodedPolyline)
            && await _routePolyline.RegenerateAsync(route, ct))
        {
            route.UpdatedAt = _clock.UtcNow;
            _db.Update(route);
            await _db.SaveChangesAsync(ct);
        }

        if (!string.IsNullOrWhiteSpace(route.EncodedPolyline))
        {
            var decoded = _matching.DecodePolyline(route.EncodedPolyline);
            if (decoded.Count >= 2)
            {
                return decoded;
            }
        }

        var points = new List<GeoCoordinate>
        {
            new(route.StartLatitude, route.StartLongitude)
        };
        points.AddRange(await _db.Stops
            .Where(s => s.RouteId == route.Id)
            .OrderBy(s => s.Order)
            .Select(s => new GeoCoordinate(s.Latitude, s.Longitude))
            .ToListAsync(ct));
        points.Add(new GeoCoordinate(route.EndLatitude, route.EndLongitude));
        return points;
    }

    private async Task<IReadOnlyList<FleetAvailabilityDto>> LoadFleetAsync(CancellationToken ct)
    {
        var vehicles = await _db.Vehicles
            .Where(v => !v.IsDeleted && v.IsActive)
            .Select(v => new
            {
                v.Id,
                v.PlateNumber,
                v.Model,
                v.Type,
                v.Capacity,
                DriverId = _db.Drivers
                    .Where(d => d.VehicleId == v.Id && !d.IsDeleted && d.IsActive)
                    .Select(d => (Guid?)d.Id)
                    .FirstOrDefault(),
                DriverName = _db.Drivers
                    .Where(d => d.VehicleId == v.Id && !d.IsDeleted && d.IsActive)
                    .Select(d => d.User.FullName ?? d.User.Phone)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        return vehicles
            .GroupBy(v => VehicleSlug(v.Type))
            .Select(g => new FleetAvailabilityDto(
                g.Key,
                g.Count(),
                g.Sum(x => x.Capacity),
                g.OrderByDescending(x => x.Capacity)
                    .Select(x => new FleetVehicleItemDto(
                        x.Id,
                        x.PlateNumber,
                        x.Model,
                        x.Capacity,
                        x.DriverId,
                        x.DriverName))
                    .ToList()))
            .OrderBy(f => f.VehicleType)
            .ToList();
    }

    private static int ParseWeeklyCount(string? notes)
    {
        var parsed = AdminMapper.ParseRouteRequestNotes(notes);
        return parsed.WeeklyCount is null or < 1 ? 1 : parsed.WeeklyCount.Value;
    }

    private static string NormalizeVehicle(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "car" or "carshuttle" or "car_shuttle" => "carshuttle",
            "bus" => "bus",
            _ => "minibus",
        };

    private static string VehicleSlug(VehicleType type) => type switch
    {
        VehicleType.Bus => "bus",
        VehicleType.CarShuttle => "carshuttle",
        _ => "minibus",
    };

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        const double r = 6_371_000;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return 2 * r * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;
}
