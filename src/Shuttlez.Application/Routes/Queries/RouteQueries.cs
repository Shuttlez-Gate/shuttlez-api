using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.Routes.DTOs;
using Shuttlez.Domain.Entities;

namespace Shuttlez.Application.Routes.Queries;

public record GetRoutesQuery : IRequest<IReadOnlyList<RouteListItemDto>>;

public record GetRouteTimelineQuery(Guid RouteId) : IRequest<RouteTimelineDto>;

public class RouteHandlers :
    IRequestHandler<GetRoutesQuery, IReadOnlyList<RouteListItemDto>>,
    IRequestHandler<GetRouteTimelineQuery, RouteTimelineDto>
{
    private static readonly (int Start, int End)[] BadgePalette =
    [
        (unchecked((int)0xFF95FEB3), unchecked((int)0xFF42FF78)),
        (unchecked((int)0xFF94F3FE), unchecked((int)0xFF30E9FF)),
        (unchecked((int)0xFFFFD5D1), unchecked((int)0xFFFF7A6E)),
    ];

    private readonly IAppDbContext _db;

    public RouteHandlers(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<RouteListItemDto>> Handle(
        GetRoutesQuery request,
        CancellationToken cancellationToken)
    {
        var routes = await _db.Routes
            .Where(r => r.IsActive && !r.IsDeleted)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        var routeIds = routes.Select(r => r.Id).ToList();
        var stopCounts = await _db.Stops
            .Where(s => routeIds.Contains(s.RouteId) && !s.IsDeleted)
            .GroupBy(s => s.RouteId)
            .Select(g => new { RouteId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RouteId, x => x.Count, cancellationToken);

        var firstStops = await _db.Stops
            .Where(s => routeIds.Contains(s.RouteId) && !s.IsDeleted)
            .OrderBy(s => s.Order)
            .ToListAsync(cancellationToken);

        var firstStopByRoute = firstStops
            .GroupBy(s => s.RouteId)
            .ToDictionary(g => g.Key, g => g.First());

        var plates = await _db.Trips
            .Where(t => routeIds.Contains(t.RouteId) && t.DriverId != null)
            .OrderByDescending(t => t.ScheduledAt)
            .Select(t => new { t.RouteId, Plate = t.Driver!.Vehicle!.PlateNumber })
            .ToListAsync(cancellationToken);

        var plateByRoute = plates
            .GroupBy(p => p.RouteId)
            .ToDictionary(g => g.Key, g => g.First().Plate);

        var items = new List<RouteListItemDto>();
        for (var i = 0; i < routes.Count; i++)
        {
            var route = routes[i];
            var (from, to) = SplitRouteName(route.Name);
            firstStopByRoute.TryGetValue(route.Id, out var firstStop);
            stopCounts.TryGetValue(route.Id, out var stopsCount);
            plateByRoute.TryGetValue(route.Id, out var plate);

            var palette = BadgePalette[i % BadgePalette.Length];
            var street = firstStop?.Name ?? route.Description ?? from;

            items.Add(new RouteListItemDto(
                route.Id,
                from,
                to,
                stopsCount,
                "أقرب نقطة التقاء - 10دق",
                street,
                plate ?? "B-YT-5904",
                palette.Start,
                palette.End));
        }

        return items;
    }

    public async Task<RouteTimelineDto> Handle(
        GetRouteTimelineQuery request,
        CancellationToken cancellationToken)
    {
        var route = await _db.Routes
            .FirstOrDefaultAsync(
                r => r.Id == request.RouteId && r.IsActive && !r.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException("المسار غير موجود");

        var stops = await _db.Stops
            .Where(s => s.RouteId == route.Id && !s.IsDeleted)
            .OrderBy(s => s.Order)
            .ToListAsync(cancellationToken);

        var (from, to) = SplitRouteName(route.Name);
        var timelineStops = BuildTimelineStops(from, to, stops);

        return new RouteTimelineDto(route.Id, timelineStops);
    }

    private static List<RouteTimelineStopDto> BuildTimelineStops(
        string from,
        string to,
        IReadOnlyList<Stop> stops)
    {
        if (stops.Count == 0)
        {
            return
            [
                new RouteTimelineStopDto("start", $"{to} - نقطة الانطلاق", "10 دقائق", "الأقرب"),
                new RouteTimelineStopDto("end", $"{from} - نقطة الوصول", "10 دقائق", "الأقرب"),
            ];
        }

        var ordered = stops.OrderBy(s => s.Order).ToList();
        var first = ordered[0];
        var last = ordered[^1];
        var result = new List<RouteTimelineStopDto>
        {
            new(
                "start",
                $"{to} - {last.Name}",
                $"10 دقائق - {last.Name}",
                "الأقرب"),
        };

        foreach (var stop in ordered.Skip(1).Take(ordered.Count - 2))
        {
            result.Add(new RouteTimelineStopDto(
                "middle",
                stop.Name,
                $"أمام {stop.Name}",
                null));
        }

        result.Add(new RouteTimelineStopDto(
            "end",
            $"{from} - {first.Name}",
            $"10 دقائق - {first.Name}",
            "الأقرب"));

        return result;
    }

    private static (string From, string To) SplitRouteName(string name)
    {
        var parts = name.Split(" - ", 2, StringSplitOptions.TrimEntries);
        if (parts.Length == 2)
            return (parts[0], parts[1]);

        return (name, name);
    }
}
