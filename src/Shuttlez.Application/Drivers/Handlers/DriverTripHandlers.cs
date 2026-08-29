using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.Drivers.Commands;
using Shuttlez.Application.Drivers.DTOs;
using Shuttlez.Application.Drivers.Queries;
using Shuttlez.Application.Notifications;
using Shuttlez.Application.Trips;
using Shuttlez.Domain.Entities;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Drivers.Handlers;

public class DriverTripHandlers :
    IRequestHandler<GetMyDriverTripsQuery, DriverTripListResponseDto>,
    IRequestHandler<StartMyDriverTripCommand, DriverTripLifecycleDto>,
    IRequestHandler<CompleteMyDriverTripCommand, DriverTripLifecycleDto>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(20);

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IMemoryCache _cache;
    private readonly IDriverRealtimeNotifier _realtime;
    private readonly TripPushNotifier _push;

    public DriverTripHandlers(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IMemoryCache cache,
        IDriverRealtimeNotifier realtime,
        TripPushNotifier push)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _cache = cache;
        _realtime = realtime;
        _push = push;
    }

    public async Task<DriverTripListResponseDto> Handle(
        GetMyDriverTripsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = RequireDriverUserId();
        var cacheKey = CacheKey(userId);
        if (_cache.TryGetValue(cacheKey, out DriverTripListResponseDto? cached) && cached is not null)
            return cached;

        var driver = await LoadDriverProjectionAsync(userId, cancellationToken);
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
                t.PricePerSeat,
                t.AvailableSeats,
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
                    .Sum(b => (decimal?)(b.PricePerSeat > 0 ? b.CaptainEarnings : b.TotalAmount)) ?? 0m
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
            ResolveUiStatus(t.Status, t.ScheduledAt, now),
            t.ScheduledAt,
            t.RouteId.ToString(),
            VehicleAssetFromType(driver.VehicleType),
            t.Status.ToString(),
            t.PricePerSeat,
            t.AvailableSeats)).ToList();

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

    public Task<DriverTripLifecycleDto> Handle(
        StartMyDriverTripCommand request,
        CancellationToken cancellationToken) =>
        TransitionAsync(
            request.TripId,
            start: true,
            cancellationToken);

    public Task<DriverTripLifecycleDto> Handle(
        CompleteMyDriverTripCommand request,
        CancellationToken cancellationToken) =>
        TransitionAsync(
            request.TripId,
            start: false,
            cancellationToken);

    private async Task<DriverTripLifecycleDto> TransitionAsync(
        Guid tripId,
        bool start,
        CancellationToken cancellationToken)
    {
        var userId = RequireDriverUserId();
        var driver = await LoadDriverProjectionAsync(userId, cancellationToken);
        var lockKey = BuildTripLockKey(tripId);

        var result = await _db.ExecuteInSerializableTransactionAsync(async ct =>
        {
            await _db.AcquireTransactionAdvisoryLockAsync(lockKey, ct);

            var trip = await _db.Trips
                .FirstOrDefaultAsync(t => t.Id == tripId && !t.IsDeleted, ct)
                ?? throw new NotFoundException("الرحلة غير موجودة", ErrorCodes.TripNotBookable);

            if (trip.DriverId is null || trip.DriverId == Guid.Empty)
            {
                throw new AppException(
                    "الرحلة غير مسندة إلى كابتن.",
                    400,
                    ErrorCodes.TripNotAssigned);
            }

            if (trip.DriverId != driver.Id)
            {
                throw new ForbiddenAppException(
                    "الرحلة غير مسندة إليك.",
                    ErrorCodes.TripNotAssigned);
            }

            if (trip.Status == TripStatus.Cancelled)
            {
                throw new AppException(
                    "تم إلغاء هذه الرحلة.",
                    400,
                    ErrorCodes.TripCancelled);
            }

            var now = _clock.UtcNow;

            if (start)
            {
                if (TripLifecycleRules.IsAlreadyStarted(trip.Status))
                {
                    return MapLifecycle(trip, "الرحلة قيد التنفيذ بالفعل.", idempotent: true);
                }

                if (!TripLifecycleRules.CanStart(trip.Status))
                {
                    throw new AppException(
                        "لا يمكن بدء الرحلة في حالتها الحالية.",
                        400,
                        ErrorCodes.TripNotStartable);
                }

                trip.Status = TripStatus.InProgress;
                trip.StartedAt ??= now;
                trip.UpdatedAt = now;
                _db.Update(trip);
                await _db.SaveChangesAsync(ct);
                return MapLifecycle(trip, "تم بدء الرحلة.");
            }

            if (TripLifecycleRules.IsAlreadyCompleted(trip.Status))
            {
                return MapLifecycle(trip, "الرحلة مكتملة بالفعل.", idempotent: true);
            }

            if (!TripLifecycleRules.CanComplete(trip.Status))
            {
                throw new AppException(
                    "لا يمكن إنهاء الرحلة في حالتها الحالية.",
                    400,
                    ErrorCodes.TripNotCompletable);
            }

            trip.Status = TripStatus.Completed;
            trip.CompletedAt ??= now;
            trip.UpdatedAt = now;
            _db.Update(trip);
            await _db.SaveChangesAsync(ct);
            return MapLifecycle(trip, "تم إنهاء الرحلة.");
        }, cancellationToken);

        _cache.Remove(CacheKey(userId));
        await _realtime.NotifyTripsChangedAsync(userId, cancellationToken);

        if (start && string.Equals(result.TripStatus, nameof(TripStatus.InProgress), StringComparison.Ordinal))
        {
            await _push.NotifyRidersTripStartedAsync(
                result.TripId, result.RouteId, result.ScheduledAt, cancellationToken);
        }
        else if (!start && string.Equals(result.TripStatus, nameof(TripStatus.Completed), StringComparison.Ordinal))
        {
            await _push.NotifyRidersTripCompletedAsync(
                result.TripId, result.RouteId, result.ScheduledAt, cancellationToken);
        }

        return result;
    }

    private Guid RequireDriverUserId()
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAppException("غير مصرح");

        if (!string.Equals(_currentUser.Role, UserType.Driver.ToString(), StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenAppException("هذا الإجراء متاح للكباتن فقط");

        return userId;
    }

    private async Task<(Guid Id, VehicleType? VehicleType)> LoadDriverProjectionAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var driver = await _db.Drivers
            .AsNoTracking()
            .Where(d => d.UserId == userId && !d.IsDeleted)
            .Select(d => new
            {
                d.Id,
                VehicleType = d.Vehicle != null ? (VehicleType?)d.Vehicle.Type : null
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("الكابتن غير موجود", ErrorCodes.DriverNotFound);

        return (driver.Id, driver.VehicleType);
    }

    private DriverTripLifecycleDto MapLifecycle(Trip trip, string message, bool idempotent = false)
    {
        var now = _clock.UtcNow;
        return new DriverTripLifecycleDto(
            trip.Id,
            trip.RouteId,
            trip.DriverId,
            trip.Status.ToString(),
            ResolveUiStatus(trip.Status, trip.ScheduledAt, now),
            trip.ScheduledAt,
            trip.StartedAt,
            trip.CompletedAt,
            trip.PricePerSeat,
            trip.AvailableSeats,
            trip.UpdatedAt,
            idempotent ? message : message);
    }

    private static string CacheKey(Guid userId) => $"driver-trips:{userId}";

    private static long BuildTripLockKey(Guid tripId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("trip-lifecycle|" + tripId.ToString("N")));
        return BitConverter.ToInt64(hash, 0);
    }

    private static string ResolveUiStatus(TripStatus status, DateTime scheduledAt, DateTime now)
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
