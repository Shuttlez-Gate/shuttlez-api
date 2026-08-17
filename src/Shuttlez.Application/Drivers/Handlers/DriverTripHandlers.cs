using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.Drivers.DTOs;
using Shuttlez.Application.Drivers.Queries;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Drivers.Handlers;

public class DriverTripHandlers : IRequestHandler<GetMyDriverTripsQuery, DriverTripListResponseDto>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(20);

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IMemoryCache _cache;

    public DriverTripHandlers(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IMemoryCache cache)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _cache = cache;
    }

    public async Task<DriverTripListResponseDto> Handle(
        GetMyDriverTripsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAppException("غير مصرح");

        if (!string.Equals(_currentUser.Role, UserType.Driver.ToString(), StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenAppException("هذا الإجراء متاح للكباتن فقط");

        var cacheKey = $"driver-trips:{userId}";
        if (_cache.TryGetValue(cacheKey, out DriverTripListResponseDto? cached) && cached is not null)
        {
            return cached;
        }

        var driver = await _db.Drivers
            .AsNoTracking()
            .Where(d => d.UserId == userId && !d.IsDeleted)
            .Select(d => new
            {
                d.Id,
                VehicleType = d.Vehicle != null ? (VehicleType?)d.Vehicle.Type : null
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("الكابتن غير موجود");

        var now = _clock.UtcNow;

        var rows = await _db.Trips
            .AsNoTracking()
            .Where(t => t.DriverId == driver.Id && !t.IsDeleted)
            .OrderBy(t => t.ScheduledAt)
            .Select(t => new
            {
                t.Id,
                t.ScheduledAt,
                t.Status,
                t.RouteId,
                From = t.Route.Name,
                To = t.Route.Stops
                    .Where(s => !s.IsDeleted)
                    .OrderByDescending(s => s.Order)
                    .Select(s => s.Name)
                    .FirstOrDefault()
                    ?? t.Route.Description
                    ?? "الوصول",
                Stations = t.Route.Stops.Count(s => !s.IsDeleted),
                Passengers = t.Bookings
                    .Where(b => !b.IsDeleted && b.Status == BookingStatus.Confirmed)
                    .Sum(b => (int?)b.SeatCount) ?? 0,
                Earnings = t.Bookings
                    .Where(b => !b.IsDeleted && b.Status == BookingStatus.Confirmed)
                    .Sum(b => (decimal?)b.TotalAmount) ?? 0m
            })
            .ToListAsync(cancellationToken);

        var projected = rows.Select(t => new DriverTripCardDto(
            t.Id,
            t.ScheduledAt.ToLocalTime().ToString("d/M/yyyy"),
            t.ScheduledAt.ToLocalTime().ToString("hh:mm tt"),
            VehicleTypeLabel(driver.VehicleType),
            t.From ?? "الانطلاق",
            t.To,
            t.Passengers,
            t.Stations,
            (double)t.Earnings,
            ResolveStatus(t.Status, t.ScheduledAt, now),
            t.ScheduledAt,
            t.RouteId.ToString(),
            VehicleAssetFromType(driver.VehicleType))).ToList();

        var response = new DriverTripListResponseDto(
            projected
                .Where(t => t.Status is "active" or "overdue")
                .OrderBy(t => t.ScheduledAt)
                .FirstOrDefault(),
            projected
                .Where(t => t.Status == "upcoming")
                .OrderBy(t => t.ScheduledAt)
                .ToList(),
            projected
                .Where(t => t.Status == "finished")
                .OrderByDescending(t => t.ScheduledAt)
                .ToList());

        _cache.Set(cacheKey, response, CacheTtl);
        return response;
    }

    private static string ResolveStatus(TripStatus status, DateTime scheduledAt, DateTime now)
    {
        if (status == TripStatus.InProgress)
            return "active";

        if (status == TripStatus.Completed || status == TripStatus.Cancelled)
            return "finished";

        if (scheduledAt <= now)
            return "overdue";

        return "upcoming";
    }

    private static string VehicleTypeLabel(VehicleType? type) => type switch
    {
        VehicleType.CarShuttle => "عربية شاتيل",
        VehicleType.Bus => "اتوبيس شاتيل",
        _ => "ميني باص"
    };

    private static string? VehicleAssetFromType(VehicleType? type) => type switch
    {
        VehicleType.CarShuttle => "car",
        VehicleType.Bus => "bus",
        VehicleType.MiniBus => "miniBus",
        _ => "car"
    };
}
