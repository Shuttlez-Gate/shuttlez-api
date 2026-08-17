using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Admin.Services;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Domain.Entities;

namespace Shuttlez.Application.Admin.Handlers;

public record AdminRoutesQuery(
    string? Search = null,
    bool? IsActive = null,
    int? Page = null,
    int? PageSize = null) : IRequest<PagedResult<AdminRouteDto>>;

public record AdminRouteDetailsQuery(Guid Id) : IRequest<AdminRouteDetailsDto>;

public record SaveRouteCommand(Guid? Id, SaveRouteRequest Request) : IRequest<AdminRouteDetailsDto>;

public record DeleteRouteCommand(Guid Id) : IRequest<bool>;

/// <summary>يستبدل كل محطات الخط بالقائمة المُرسلة (أبسط وأدق من تعديل عنصر بعنصر).</summary>
public record ReplaceStopsCommand(Guid RouteId, IReadOnlyList<SaveStopRequest> Stops)
    : IRequest<AdminRouteDetailsDto>;

public class AdminRouteHandlers :
    IRequestHandler<AdminRoutesQuery, PagedResult<AdminRouteDto>>,
    IRequestHandler<AdminRouteDetailsQuery, AdminRouteDetailsDto>,
    IRequestHandler<SaveRouteCommand, AdminRouteDetailsDto>,
    IRequestHandler<DeleteRouteCommand, bool>,
    IRequestHandler<ReplaceStopsCommand, AdminRouteDetailsDto>
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly IRoutePolylineService _polyline;

    public AdminRouteHandlers(
        IAppDbContext db,
        IDateTimeProvider clock,
        IRoutePolylineService polyline)
    {
        _db = db;
        _clock = clock;
        _polyline = polyline;
    }

    public async Task<PagedResult<AdminRouteDto>> Handle(
        AdminRoutesQuery request,
        CancellationToken cancellationToken)
    {
        var page = PageRequest.From(request.Page, request.PageSize);
        var query = _db.Routes.Where(r => !r.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(r =>
                r.Name.Contains(term)
                || (r.Description != null && r.Description.Contains(term)));
        }

        if (request.IsActive is not null)
        {
            query = query.Where(r => r.IsActive == request.IsActive);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(Projection())
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminRouteDto>(items, page.Page, page.PageSize, total);
    }

    public async Task<AdminRouteDetailsDto> Handle(
        AdminRouteDetailsQuery request,
        CancellationToken cancellationToken) =>
        await LoadDetailsAsync(request.Id, cancellationToken);

    public async Task<AdminRouteDetailsDto> Handle(
        SaveRouteCommand request,
        CancellationToken cancellationToken)
    {
        var body = request.Request;
        if (string.IsNullOrWhiteSpace(body.Name))
        {
            throw new AppException("اسم الخط مطلوب");
        }

        Route route;
        if (request.Id is null)
        {
            route = new Route();
            _db.Add(route);
        }
        else
        {
            route = await _db.Routes
                .FirstOrDefaultAsync(r => r.Id == request.Id && !r.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("الخط غير موجود");
            route.UpdatedAt = _clock.UtcNow;
            _db.Update(route);
        }

        route.Name = body.Name.Trim();
        route.Description = body.Description?.Trim();
        route.StartLatitude = body.StartLatitude;
        route.StartLongitude = body.StartLongitude;
        route.EndLatitude = body.EndLatitude;
        route.EndLongitude = body.EndLongitude;
        route.EncodedPolyline = string.IsNullOrWhiteSpace(body.EncodedPolyline)
            ? null
            : body.EncodedPolyline.Trim();
        route.DistanceMeters = body.DistanceMeters;
        route.DurationSeconds = body.DurationSeconds;
        route.IsActive = body.IsActive;

        route.BoundsMinLatitude = Math.Min(body.StartLatitude, body.EndLatitude);
        route.BoundsMaxLatitude = Math.Max(body.StartLatitude, body.EndLatitude);
        route.BoundsMinLongitude = Math.Min(body.StartLongitude, body.EndLongitude);
        route.BoundsMaxLongitude = Math.Max(body.StartLongitude, body.EndLongitude);

        if (string.IsNullOrWhiteSpace(route.EncodedPolyline))
        {
            await _polyline.RegenerateAsync(route, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await LoadDetailsAsync(route.Id, cancellationToken);
    }

    public async Task<bool> Handle(
        DeleteRouteCommand request,
        CancellationToken cancellationToken)
    {
        var route = await _db.Routes
            .FirstOrDefaultAsync(r => r.Id == request.Id && !r.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("الخط غير موجود");

        var hasTrips = await _db.Trips.AnyAsync(
            t => t.RouteId == route.Id && !t.IsDeleted, cancellationToken);
        if (hasTrips)
        {
            throw new AppException("لا يمكن حذف خط له رحلات. عطّله بدلاً من ذلك");
        }

        route.IsDeleted = true;
        route.IsActive = false;
        route.UpdatedAt = _clock.UtcNow;
        _db.Update(route);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<AdminRouteDetailsDto> Handle(
        ReplaceStopsCommand request,
        CancellationToken cancellationToken)
    {
        var route = await _db.Routes
            .FirstOrDefaultAsync(r => r.Id == request.RouteId && !r.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("الخط غير موجود");

        var current = await _db.Stops
            .Where(s => s.RouteId == request.RouteId)
            .ToListAsync(cancellationToken);

        foreach (var stop in current)
        {
            _db.Remove(stop);
        }

        var order = 1;
        foreach (var stop in request.Stops.OrderBy(s => s.Order))
        {
            if (string.IsNullOrWhiteSpace(stop.Name))
            {
                throw new AppException("اسم المحطة مطلوب");
            }

            _db.Add(new Stop
            {
                RouteId = request.RouteId,
                Name = stop.Name.Trim(),
                Latitude = stop.Latitude,
                Longitude = stop.Longitude,
                Order = order++
            });
        }

        // المحطات تغيّرت → أعد توليد مسار الخط ليمر عليها.
        await _db.SaveChangesAsync(cancellationToken);
        if (await _polyline.RegenerateAsync(route, cancellationToken))
        {
            route.UpdatedAt = _clock.UtcNow;
            _db.Update(route);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return await LoadDetailsAsync(request.RouteId, cancellationToken);
    }

    private async Task<AdminRouteDetailsDto> LoadDetailsAsync(Guid id, CancellationToken ct)
    {
        var route = await _db.Routes
            .Where(r => r.Id == id && !r.IsDeleted)
            .Select(Projection())
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("الخط غير موجود");

        var stops = await _db.Stops
            .Where(s => s.RouteId == id && !s.IsDeleted)
            .OrderBy(s => s.Order)
            .Select(s => new AdminStopDto(s.Id, s.Name, s.Latitude, s.Longitude, s.Order))
            .ToListAsync(ct);

        return new AdminRouteDetailsDto(route, stops);
    }

    private System.Linq.Expressions.Expression<Func<Route, AdminRouteDto>> Projection() =>
        r => new AdminRouteDto(
            r.Id,
            r.Name,
            r.Description,
            r.StartLatitude,
            r.StartLongitude,
            r.EndLatitude,
            r.EndLongitude,
            r.EncodedPolyline,
            r.DistanceMeters,
            r.DurationSeconds,
            r.IsActive,
            _db.Stops.Count(s => s.RouteId == r.Id),
            _db.Trips.Count(t => t.RouteId == r.Id && !t.IsDeleted),
            r.CreatedAt);
}
