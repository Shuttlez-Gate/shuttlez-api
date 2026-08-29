using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.Notifications;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Admin.Handlers;

public record AssignTripDriverRequest(Guid DriverId);

public record AssignTripDriverCommand(Guid TripId, Guid DriverId) : IRequest<AdminTripDto>;

public record UnassignTripDriverCommand(Guid TripId) : IRequest<AdminTripDto>;

public class AdminTripDriverAssignmentHandlers :
    IRequestHandler<AssignTripDriverCommand, AdminTripDto>,
    IRequestHandler<UnassignTripDriverCommand, AdminTripDto>
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly IMemoryCache _cache;
    private readonly IDriverRealtimeNotifier _realtime;
    private readonly TripPushNotifier _push;

    public AdminTripDriverAssignmentHandlers(
        IAppDbContext db,
        IDateTimeProvider clock,
        IMemoryCache cache,
        IDriverRealtimeNotifier realtime,
        TripPushNotifier push)
    {
        _db = db;
        _clock = clock;
        _cache = cache;
        _realtime = realtime;
        _push = push;
    }

    public async Task<AdminTripDto> Handle(
        AssignTripDriverCommand request,
        CancellationToken cancellationToken)
    {
        Guid? previousDriverUserId = null;
        Guid? newDriverUserId = null;
        Guid tripId = request.TripId;
        Guid routeId = Guid.Empty;
        DateTime scheduledAt = default;
        string? routeName = null;
        Guid? previousDriverId = null;
        Guid newDriverId = request.DriverId;

        var dto = await _db.ExecuteInSerializableTransactionAsync(async ct =>
        {
            await _db.AcquireTransactionAdvisoryLockAsync(TripLockKey(request.TripId), ct);

            var trip = await _db.Trips
                .Include(t => t.Route)
                .FirstOrDefaultAsync(t => t.Id == request.TripId && !t.IsDeleted, ct)
                ?? throw new NotFoundException("الرحلة غير موجودة", ErrorCodes.TripNotFound);

            if (trip.Status == TripStatus.Cancelled)
                throw new AppException("لا يمكن تعيين كابتن لرحلة ملغاة", 400, ErrorCodes.TripCancelled);

            if (trip.Status == TripStatus.Completed)
                throw new AppException("لا يمكن تعيين كابتن لرحلة مكتملة", 400, ErrorCodes.TripAlreadyCompleted);

            if (trip.Status == TripStatus.InProgress)
                throw new AppException("لا يمكن إعادة تعيين كابتن أثناء تنفيذ الرحلة", 400, ErrorCodes.TripAlreadyStarted);

            var driver = await _db.Drivers
                .AsNoTracking()
                .Where(d => d.Id == request.DriverId && !d.IsDeleted)
                .Select(d => new { d.Id, d.UserId, d.IsActive, d.VerificationStatus })
                .FirstOrDefaultAsync(ct)
                ?? throw new NotFoundException("الكابتن غير موجود", ErrorCodes.DriverNotFound);

            if (!driver.IsActive)
                throw new AppException("الكابتن غير نشط", 400, ErrorCodes.DriverInactive);

            if (driver.VerificationStatus != DriverVerificationStatus.Approved)
                throw new AppException("الكابتن غير مؤهل للتعيين", 400, ErrorCodes.DriverNotEligible);

            var conflict = await _db.Trips.AnyAsync(
                t => t.DriverId == driver.Id &&
                     t.Id != trip.Id &&
                     !t.IsDeleted &&
                     t.Status != TripStatus.Cancelled &&
                     t.Status != TripStatus.Completed &&
                     t.ScheduledAt == trip.ScheduledAt,
                ct);

            if (conflict)
            {
                throw new AppException(
                    "الكابتن مسند لرحلة أخرى في نفس الموعد",
                    409,
                    ErrorCodes.DriverTripConflict);
            }

            previousDriverId = trip.DriverId;
            if (previousDriverId is not null && previousDriverId != driver.Id)
            {
                previousDriverUserId = await _db.Drivers
                    .AsNoTracking()
                    .Where(d => d.Id == previousDriverId && !d.IsDeleted)
                    .Select(d => (Guid?)d.UserId)
                    .FirstOrDefaultAsync(ct);
            }

            trip.DriverId = driver.Id;
            trip.Status = TripStatus.DriverAssigned;
            trip.UpdatedAt = _clock.UtcNow;
            _db.Update(trip);
            await _db.SaveChangesAsync(ct);

            newDriverUserId = driver.UserId;
            tripId = trip.Id;
            routeId = trip.RouteId;
            scheduledAt = trip.ScheduledAt;
            routeName = trip.Route?.Name;

            return await ProjectTripAsync(trip.Id, ct);
        }, cancellationToken);

        await BustDriverCacheAndRealtimeAsync(previousDriverId, cancellationToken);
        await BustDriverCacheAndRealtimeAsync(newDriverId, cancellationToken);

        if (previousDriverUserId is not null && previousDriverUserId != newDriverUserId)
        {
            await _push.NotifyCaptainUnassignedAsync(
                previousDriverUserId.Value, tripId, routeId, scheduledAt, cancellationToken);
        }

        if (newDriverUserId is not null)
        {
            await _push.NotifyCaptainAssignedAsync(
                newDriverUserId.Value, tripId, routeId, scheduledAt, routeName, cancellationToken);
        }

        return dto;
    }

    public async Task<AdminTripDto> Handle(
        UnassignTripDriverCommand request,
        CancellationToken cancellationToken)
    {
        Guid? previousDriverUserId = null;
        Guid? previousDriverId = null;
        Guid tripId = request.TripId;
        Guid routeId = Guid.Empty;
        DateTime scheduledAt = default;

        var dto = await _db.ExecuteInSerializableTransactionAsync(async ct =>
        {
            await _db.AcquireTransactionAdvisoryLockAsync(TripLockKey(request.TripId), ct);

            var trip = await _db.Trips
                .FirstOrDefaultAsync(t => t.Id == request.TripId && !t.IsDeleted, ct)
                ?? throw new NotFoundException("الرحلة غير موجودة", ErrorCodes.TripNotFound);

            if (trip.Status is TripStatus.Cancelled)
                throw new AppException("الرحلة ملغاة", 400, ErrorCodes.TripCancelled);

            if (trip.Status is TripStatus.Completed)
                throw new AppException("الرحلة مكتملة", 400, ErrorCodes.TripAlreadyCompleted);

            if (trip.Status is TripStatus.InProgress)
                throw new AppException("لا يمكن إلغاء التعيين أثناء تنفيذ الرحلة", 400, ErrorCodes.TripAlreadyStarted);

            previousDriverId = trip.DriverId;
            if (previousDriverId is not null)
            {
                previousDriverUserId = await _db.Drivers
                    .AsNoTracking()
                    .Where(d => d.Id == previousDriverId && !d.IsDeleted)
                    .Select(d => (Guid?)d.UserId)
                    .FirstOrDefaultAsync(ct);
            }

            trip.DriverId = null;
            if (trip.Status is TripStatus.DriverAssigned or TripStatus.Scheduled)
            {
                trip.Status = TripStatus.Scheduled;
            }

            trip.UpdatedAt = _clock.UtcNow;
            _db.Update(trip);
            await _db.SaveChangesAsync(ct);

            tripId = trip.Id;
            routeId = trip.RouteId;
            scheduledAt = trip.ScheduledAt;

            return await ProjectTripAsync(trip.Id, ct);
        }, cancellationToken);

        await BustDriverCacheAndRealtimeAsync(previousDriverId, cancellationToken);

        if (previousDriverUserId is not null)
        {
            await _push.NotifyCaptainUnassignedAsync(
                previousDriverUserId.Value, tripId, routeId, scheduledAt, cancellationToken);
        }

        return dto;
    }

    private async Task<AdminTripDto> ProjectTripAsync(Guid tripId, CancellationToken ct) =>
        await _db.Trips
            .AsNoTracking()
            .Where(t => t.Id == tripId)
            .Select(t => new AdminTripDto(
                t.Id,
                t.RouteId,
                t.Route.Name,
                t.DriverId,
                t.Driver == null ? null : (t.Driver.User.FullName ?? t.Driver.User.Phone),
                t.Status == TripStatus.Scheduled ? "scheduled"
                    : t.Status == TripStatus.DriverAssigned ? "driverassigned"
                    : t.Status == TripStatus.InProgress ? "inprogress"
                    : t.Status == TripStatus.Completed ? "completed" : "cancelled",
                t.ScheduledAt,
                t.StartedAt,
                t.CompletedAt,
                t.PricePerSeat,
                t.AvailableSeats,
                t.ReferenceCode,
                _db.Bookings.Count(b => b.TripId == t.Id && !b.IsDeleted),
                _db.Bookings
                    .Where(b => b.TripId == t.Id && !b.IsDeleted && b.Status == BookingStatus.Confirmed)
                    .Sum(b => (decimal?)b.TotalAmount) ?? 0m,
                t.CreatedAt))
            .FirstAsync(ct);

    private async Task BustDriverCacheAndRealtimeAsync(Guid? driverId, CancellationToken ct)
    {
        if (driverId is null || driverId == Guid.Empty) return;

        var userId = await _db.Drivers
            .AsNoTracking()
            .Where(d => d.Id == driverId && !d.IsDeleted)
            .Select(d => (Guid?)d.UserId)
            .FirstOrDefaultAsync(ct);

        if (userId is null) return;

        _cache.Remove($"driver-trips:{userId}");
        await _realtime.NotifyTripsChangedAsync(userId.Value, ct);
    }

    private static long TripLockKey(Guid tripId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("trip-assign|" + tripId.ToString("N")));
        return BitConverter.ToInt64(hash, 0);
    }
}
