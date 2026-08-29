using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.Notifications;
using Shuttlez.Application.Rides;
using Shuttlez.Application.Rides.DTOs;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Rides.Handlers;

public record AdminRidesQuery(
    string? Status = null,
    Guid? DriverId = null,
    Guid? RiderUserId = null,
    int? Page = null,
    int? PageSize = null) : IRequest<PagedResult<RideDto>>;

public record AssignRideDriverCommand(Guid RideId, Guid DriverId) : IRequest<RideDto>;

public record UnassignRideDriverCommand(Guid RideId) : IRequest<RideDto>;

public class AdminRideHandlers :
    IRequestHandler<AdminRidesQuery, PagedResult<RideDto>>,
    IRequestHandler<AssignRideDriverCommand, RideDto>,
    IRequestHandler<UnassignRideDriverCommand, RideDto>
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly RidePushNotifier _push;

    public AdminRideHandlers(IAppDbContext db, IDateTimeProvider clock, RidePushNotifier push)
    {
        _db = db;
        _clock = clock;
        _push = push;
    }

    public async Task<PagedResult<RideDto>> Handle(
        AdminRidesQuery request,
        CancellationToken cancellationToken)
    {
        var page = PageRequest.From(request.Page, request.PageSize);
        var query = _db.RideRequests.AsNoTracking().Where(r => !r.IsDeleted);

        if (request.DriverId is not null)
            query = query.Where(r => r.DriverId == request.DriverId);

        if (request.RiderUserId is not null)
            query = query.Where(r => r.RiderUserId == request.RiderUserId);

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<RideRequestStatus>(request.Status, true, out var status))
        {
            query = query.Where(r => r.Status == status);
        }

        var total = await query.CountAsync(cancellationToken);
        var ids = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var items = new List<RideDto>(ids.Count);
        foreach (var id in ids)
            items.Add(await ProjectAsync(id, cancellationToken));

        return new PagedResult<RideDto>(items, page.Page, page.PageSize, total);
    }

    public async Task<RideDto> Handle(
        AssignRideDriverCommand request,
        CancellationToken cancellationToken)
    {
        Guid riderUserId = Guid.Empty;

        var dto = await _db.ExecuteInSerializableTransactionAsync(async ct =>
        {
            await _db.AcquireTransactionAdvisoryLockAsync(RideLockKey(request.RideId), ct);

            var ride = await _db.RideRequests
                .FirstOrDefaultAsync(r => r.Id == request.RideId && !r.IsDeleted, ct)
                ?? throw new NotFoundException("المشوار غير موجود", ErrorCodes.RideNotFound);

            if (ride.Status is RideRequestStatus.Cancelled)
                throw new AppException("لا يمكن تعيين كابتن لمشوار ملغى", 400, ErrorCodes.RideCancelled);

            if (ride.Status is RideRequestStatus.Completed)
                throw new AppException("لا يمكن تعيين كابتن لمشوار مكتمل", 400, ErrorCodes.RideAlreadyCompleted);

            if (ride.Status is RideRequestStatus.InProgress)
                throw new AppException("لا يمكن إعادة تعيين كابتن أثناء تنفيذ المشوار", 400, ErrorCodes.RideAlreadyStarted);

            if (ride.Status is not (RideRequestStatus.Requested or RideRequestStatus.Assigned))
                throw new AppException("لا يمكن تعيين كابتن لهذا المشوار", 400, ErrorCodes.RideNotAssignable);

            var driver = await _db.Drivers
                .AsNoTracking()
                .Where(d => d.Id == request.DriverId && !d.IsDeleted)
                .Select(d => new { d.Id, d.IsActive, d.VerificationStatus })
                .FirstOrDefaultAsync(ct)
                ?? throw new NotFoundException("الكابتن غير موجود", ErrorCodes.DriverNotFound);

            if (!driver.IsActive)
                throw new AppException("الكابتن غير نشط", 400, ErrorCodes.DriverInactive);

            if (driver.VerificationStatus != DriverVerificationStatus.Approved)
                throw new AppException("الكابتن غير مؤهل للتعيين", 400, ErrorCodes.DriverNotEligible);

            var rideConflict = await _db.RideRequests.AnyAsync(
                r => r.DriverId == driver.Id &&
                     r.Id != ride.Id &&
                     !r.IsDeleted &&
                     (r.Status == RideRequestStatus.Requested ||
                      r.Status == RideRequestStatus.Assigned ||
                      r.Status == RideRequestStatus.InProgress),
                ct);

            if (rideConflict)
            {
                throw new AppException(
                    "الكابتن مسند لمشوار آخر نشط",
                    409,
                    ErrorCodes.DriverRideConflict);
            }

            var groupConflict = await _db.GroupRequests.AnyAsync(
                g => g.DriverId == driver.Id &&
                     !g.IsDeleted &&
                     (g.Status == GroupRequestStatus.Confirmed ||
                      g.Status == GroupRequestStatus.Assigned ||
                      g.Status == GroupRequestStatus.InProgress),
                ct);

            if (groupConflict)
            {
                throw new AppException(
                    "الكابتن مسند لمجموعة أخرى نشطة",
                    409,
                    ErrorCodes.DriverGroupConflict);
            }

            var now = _clock.UtcNow;
            ride.DriverId = driver.Id;
            ride.Status = RideRequestStatus.Assigned;
            ride.AssignedAt ??= now;
            ride.UpdatedAt = now;
            _db.Update(ride);
            await _db.SaveChangesAsync(ct);

            riderUserId = ride.RiderUserId;
            return await ProjectAsync(ride.Id, ct);
        }, cancellationToken);

        // Post-commit soft-fail: FCM must never roll back assignment.
        await _push.NotifyRideAssignedAsync(riderUserId, dto.Id, cancellationToken);
        return dto;
    }

    public async Task<RideDto> Handle(
        UnassignRideDriverCommand request,
        CancellationToken cancellationToken)
    {
        return await _db.ExecuteInSerializableTransactionAsync(async ct =>
        {
            await _db.AcquireTransactionAdvisoryLockAsync(RideLockKey(request.RideId), ct);

            var ride = await _db.RideRequests
                .FirstOrDefaultAsync(r => r.Id == request.RideId && !r.IsDeleted, ct)
                ?? throw new NotFoundException("المشوار غير موجود", ErrorCodes.RideNotFound);

            if (ride.Status is RideRequestStatus.Cancelled)
                throw new AppException("المشوار ملغى", 400, ErrorCodes.RideCancelled);

            if (ride.Status is RideRequestStatus.Completed)
                throw new AppException("المشوار مكتمل", 400, ErrorCodes.RideAlreadyCompleted);

            if (ride.Status is RideRequestStatus.InProgress)
                throw new AppException("لا يمكن إلغاء التعيين أثناء تنفيذ المشوار", 400, ErrorCodes.RideAlreadyStarted);

            ride.DriverId = null;
            ride.AssignedAt = null;
            if (ride.Status == RideRequestStatus.Assigned)
                ride.Status = RideRequestStatus.Requested;

            ride.UpdatedAt = _clock.UtcNow;
            _db.Update(ride);
            await _db.SaveChangesAsync(ct);
            return await ProjectAsync(ride.Id, ct);
        }, cancellationToken);
    }

    private Task<RideDto> ProjectAsync(Guid rideId, CancellationToken ct) =>
        RideDtoProjector.ProjectAsync(_db, rideId, ct);

    private static long RideLockKey(Guid rideId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("ride-assign|" + rideId.ToString("N")));
        return BitConverter.ToInt64(hash, 0);
    }
}
