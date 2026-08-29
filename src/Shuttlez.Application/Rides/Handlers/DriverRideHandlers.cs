using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.Rides;
using Shuttlez.Application.Rides.DTOs;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Rides.Handlers;

public record GetMyDriverRidesQuery : IRequest<IReadOnlyList<RideDto>>;

public record StartMyDriverRideCommand(Guid RideId) : IRequest<DriverRideLifecycleDto>;

public record CompleteMyDriverRideCommand(Guid RideId) : IRequest<DriverRideLifecycleDto>;

public record UpdateMyDriverRideLocationCommand(
    Guid RideId,
    double Latitude,
    double Longitude) : IRequest<DriverRideLocationDto>;

public class DriverRideHandlers :
    IRequestHandler<GetMyDriverRidesQuery, IReadOnlyList<RideDto>>,
    IRequestHandler<StartMyDriverRideCommand, DriverRideLifecycleDto>,
    IRequestHandler<CompleteMyDriverRideCommand, DriverRideLifecycleDto>,
    IRequestHandler<UpdateMyDriverRideLocationCommand, DriverRideLocationDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public DriverRideHandlers(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<IReadOnlyList<RideDto>> Handle(
        GetMyDriverRidesQuery request,
        CancellationToken cancellationToken)
    {
        var driverId = await RequireDriverIdAsync(cancellationToken);
        var ids = await _db.RideRequests
            .AsNoTracking()
            .Where(r => !r.IsDeleted && r.DriverId == driverId)
            .OrderByDescending(r => r.AssignedAt ?? r.CreatedAt)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var list = new List<RideDto>(ids.Count);
        foreach (var id in ids)
            list.Add(await ProjectAsync(id, cancellationToken));
        return list;
    }

    public Task<DriverRideLifecycleDto> Handle(
        StartMyDriverRideCommand request,
        CancellationToken cancellationToken) =>
        TransitionAsync(request.RideId, start: true, cancellationToken);

    public Task<DriverRideLifecycleDto> Handle(
        CompleteMyDriverRideCommand request,
        CancellationToken cancellationToken) =>
        TransitionAsync(request.RideId, start: false, cancellationToken);

    public async Task<DriverRideLocationDto> Handle(
        UpdateMyDriverRideLocationCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Latitude is < -90 or > 90 ||
            request.Longitude is < -180 or > 180)
        {
            throw new AppException(
                "إحداثيات الموقع غير صالحة.",
                400,
                ErrorCodes.RideLocationInvalid);
        }

        var driverId = await RequireDriverIdAsync(cancellationToken);

        return await _db.ExecuteInSerializableTransactionAsync(async ct =>
        {
            var ride = await _db.RideRequests
                .FirstOrDefaultAsync(r => r.Id == request.RideId && !r.IsDeleted, ct)
                ?? throw new NotFoundException("المشوار غير موجود", ErrorCodes.RideNotFound);

            if (ride.DriverId is null || ride.DriverId == Guid.Empty)
            {
                throw new AppException(
                    "المشوار غير مسند إلى كابتن.",
                    400,
                    ErrorCodes.RideNotAssigned);
            }

            if (ride.DriverId != driverId)
                throw new ForbiddenAppException("المشوار غير مسند إليك.", ErrorCodes.RideNotAssigned);

            if (ride.Status is RideRequestStatus.Completed or RideRequestStatus.Cancelled)
            {
                throw new AppException(
                    "لا يمكن تحديث الموقع بعد انتهاء أو إلغاء المشوار.",
                    400,
                    ErrorCodes.RideLocationNotAllowed);
            }

            if (ride.Status is not (RideRequestStatus.Assigned or RideRequestStatus.InProgress))
            {
                throw new AppException(
                    "تحديث الموقع متاح فقط أثناء التعيين أو الرحلة.",
                    400,
                    ErrorCodes.RideLocationNotAllowed);
            }

            var now = _clock.UtcNow;
            ride.CaptainLatitude = request.Latitude;
            ride.CaptainLongitude = request.Longitude;
            ride.CaptainLocationUpdatedAt = now;
            ride.UpdatedAt = now;
            _db.Update(ride);
            await _db.SaveChangesAsync(ct);

            return new DriverRideLocationDto(
                ride.Id,
                ride.CaptainLatitude!.Value,
                ride.CaptainLongitude!.Value,
                ride.CaptainLocationUpdatedAt!.Value,
                ride.Status.ToString());
        }, cancellationToken);
    }

    private async Task<DriverRideLifecycleDto> TransitionAsync(
        Guid rideId,
        bool start,
        CancellationToken cancellationToken)
    {
        var driverId = await RequireDriverIdAsync(cancellationToken);

        return await _db.ExecuteInSerializableTransactionAsync(async ct =>
        {
            await _db.AcquireTransactionAdvisoryLockAsync(RideLifecycleLockKey(rideId), ct);

            var ride = await _db.RideRequests
                .FirstOrDefaultAsync(r => r.Id == rideId && !r.IsDeleted, ct)
                ?? throw new NotFoundException("المشوار غير موجود", ErrorCodes.RideNotFound);

            if (ride.DriverId is null || ride.DriverId == Guid.Empty)
            {
                throw new AppException(
                    "المشوار غير مسند إلى كابتن.",
                    400,
                    ErrorCodes.RideNotAssigned);
            }

            if (ride.DriverId != driverId)
                throw new ForbiddenAppException("المشوار غير مسند إليك.", ErrorCodes.RideNotAssigned);

            if (ride.Status == RideRequestStatus.Cancelled)
                throw new AppException("تم إلغاء هذا المشوار.", 400, ErrorCodes.RideCancelled);

            var now = _clock.UtcNow;

            if (start)
            {
                if (ride.Status == RideRequestStatus.InProgress)
                    return MapLifecycle(ride, "المشوار قيد التنفيذ بالفعل.", idempotent: true);

                if (ride.Status is RideRequestStatus.Completed)
                    throw new AppException("المشوار مكتمل بالفعل.", 400, ErrorCodes.RideAlreadyCompleted);

                if (ride.Status != RideRequestStatus.Assigned)
                {
                    throw new AppException(
                        "لا يمكن بدء المشوار في حالته الحالية.",
                        400,
                        ErrorCodes.RideNotStartable);
                }

                ride.Status = RideRequestStatus.InProgress;
                ride.StartedAt ??= now;
                ride.UpdatedAt = now;
                _db.Update(ride);
                await _db.SaveChangesAsync(ct);
                return MapLifecycle(ride, "تم بدء المشوار.");
            }

            if (ride.Status == RideRequestStatus.Completed)
                return MapLifecycle(ride, "المشوار مكتمل بالفعل.", idempotent: true);

            if (ride.Status != RideRequestStatus.InProgress)
            {
                throw new AppException(
                    "لا يمكن إنهاء المشوار في حالته الحالية.",
                    400,
                    ErrorCodes.RideNotCompletable);
            }

            ride.Status = RideRequestStatus.Completed;
            ride.CompletedAt ??= now;
            ride.UpdatedAt = now;
            ride.CaptainLatitude = null;
            ride.CaptainLongitude = null;
            ride.CaptainLocationUpdatedAt = null;
            _db.Update(ride);
            await _db.SaveChangesAsync(ct);
            return MapLifecycle(ride, "تم إنهاء المشوار.");
        }, cancellationToken);
    }

    private async Task<Guid> RequireDriverIdAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAppException("غير مصرح");

        if (!string.Equals(_currentUser.Role, UserType.Driver.ToString(), StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenAppException("هذا الإجراء متاح للكباتن فقط");

        var driverId = await _db.Drivers
            .AsNoTracking()
            .Where(d => d.UserId == userId && !d.IsDeleted)
            .Select(d => d.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (driverId == Guid.Empty)
            throw new NotFoundException("الكابتن غير موجود", ErrorCodes.DriverNotFound);

        return driverId;
    }

    private static DriverRideLifecycleDto MapLifecycle(
        Domain.Entities.RideRequest ride,
        string message,
        bool idempotent = false) =>
        new(
            ride.Id,
            ride.DriverId,
            ride.Status.ToString(),
            ride.StartedAt,
            ride.CompletedAt,
            ride.UpdatedAt,
            message,
            idempotent);

    private Task<RideDto> ProjectAsync(Guid rideId, CancellationToken ct) =>
        RideDtoProjector.ProjectAsync(_db, rideId, ct);

    private static long RideLifecycleLockKey(Guid rideId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("ride-lifecycle|" + rideId.ToString("N")));
        return BitConverter.ToInt64(hash, 0);
    }
}
