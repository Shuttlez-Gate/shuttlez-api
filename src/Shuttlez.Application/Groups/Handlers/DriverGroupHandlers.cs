using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.Groups.DTOs;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Groups.Handlers;

public record GetMyDriverGroupsQuery : IRequest<IReadOnlyList<GroupDto>>;

public record StartMyDriverGroupCommand(Guid GroupId) : IRequest<DriverGroupLifecycleDto>;

public record CompleteMyDriverGroupCommand(Guid GroupId) : IRequest<DriverGroupLifecycleDto>;

public class DriverGroupHandlers :
    IRequestHandler<GetMyDriverGroupsQuery, IReadOnlyList<GroupDto>>,
    IRequestHandler<StartMyDriverGroupCommand, DriverGroupLifecycleDto>,
    IRequestHandler<CompleteMyDriverGroupCommand, DriverGroupLifecycleDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public DriverGroupHandlers(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<IReadOnlyList<GroupDto>> Handle(
        GetMyDriverGroupsQuery request,
        CancellationToken cancellationToken)
    {
        var driverId = await RequireDriverIdAsync(cancellationToken);
        var ids = await _db.GroupRequests
            .AsNoTracking()
            .Where(g => !g.IsDeleted && g.DriverId == driverId)
            .OrderByDescending(g => g.AssignedAt ?? g.CreatedAt)
            .Select(g => g.Id)
            .ToListAsync(cancellationToken);

        var list = new List<GroupDto>(ids.Count);
        foreach (var id in ids)
            list.Add(await ProjectAsync(id, cancellationToken));
        return list;
    }

    public Task<DriverGroupLifecycleDto> Handle(
        StartMyDriverGroupCommand request,
        CancellationToken cancellationToken) =>
        TransitionAsync(request.GroupId, start: true, cancellationToken);

    public Task<DriverGroupLifecycleDto> Handle(
        CompleteMyDriverGroupCommand request,
        CancellationToken cancellationToken) =>
        TransitionAsync(request.GroupId, start: false, cancellationToken);

    private async Task<DriverGroupLifecycleDto> TransitionAsync(
        Guid groupId,
        bool start,
        CancellationToken cancellationToken)
    {
        var driverId = await RequireDriverIdAsync(cancellationToken);

        return await _db.ExecuteInSerializableTransactionAsync(async ct =>
        {
            await _db.AcquireTransactionAdvisoryLockAsync(GroupLifecycleLockKey(groupId), ct);

            var group = await _db.GroupRequests
                .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted, ct)
                ?? throw new NotFoundException("المجموعة غير موجودة", ErrorCodes.GroupNotFound);

            if (group.DriverId is null || group.DriverId == Guid.Empty)
            {
                throw new AppException(
                    "المجموعة غير مسندة إلى كابتن.",
                    400,
                    ErrorCodes.GroupNotAssigned);
            }

            if (group.DriverId != driverId)
                throw new ForbiddenAppException("المجموعة غير مسندة إليك.", ErrorCodes.GroupNotAssigned);

            if (group.Status == GroupRequestStatus.Cancelled)
                throw new AppException("تم إلغاء هذه المجموعة.", 400, ErrorCodes.GroupNotCancellable);

            var now = _clock.UtcNow;

            if (start)
            {
                if (group.Status == GroupRequestStatus.InProgress)
                    return MapLifecycle(group, "المجموعة قيد التنفيذ بالفعل.", idempotent: true);

                if (group.Status == GroupRequestStatus.Completed)
                    throw new AppException("المجموعة مكتملة بالفعل.", 400, ErrorCodes.GroupNotCompletable);

                if (group.Status != GroupRequestStatus.Assigned)
                {
                    throw new AppException(
                        "لا يمكن بدء المجموعة في حالتها الحالية.",
                        400,
                        ErrorCodes.GroupNotStartable);
                }

                group.Status = GroupRequestStatus.InProgress;
                group.StartedAt ??= now;
                group.UpdatedAt = now;
                _db.Update(group);
                await _db.SaveChangesAsync(ct);
                return MapLifecycle(group, "تم بدء المجموعة.");
            }

            if (group.Status == GroupRequestStatus.Completed)
                return MapLifecycle(group, "المجموعة مكتملة بالفعل.", idempotent: true);

            if (group.Status != GroupRequestStatus.InProgress)
            {
                throw new AppException(
                    "لا يمكن إنهاء المجموعة في حالتها الحالية.",
                    400,
                    ErrorCodes.GroupNotCompletable);
            }

            group.Status = GroupRequestStatus.Completed;
            group.CompletedAt ??= now;
            group.UpdatedAt = now;
            _db.Update(group);
            await _db.SaveChangesAsync(ct);
            return MapLifecycle(group, "تم إنهاء المجموعة.");
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

    private static DriverGroupLifecycleDto MapLifecycle(
        Domain.Entities.GroupRequest group,
        string message,
        bool idempotent = false) =>
        new(
            group.Id,
            group.DriverId,
            group.Status.ToString(),
            group.StartedAt,
            group.CompletedAt,
            group.UpdatedAt,
            message,
            idempotent);

    private async Task<GroupDto> ProjectAsync(Guid groupId, CancellationToken ct)
    {
        var row = await _db.GroupRequests
            .AsNoTracking()
            .Where(g => g.Id == groupId && !g.IsDeleted)
            .Select(g => new
            {
                g.Id,
                g.OrganizerUserId,
                g.PickupLatitude,
                g.PickupLongitude,
                g.PickupAddress,
                g.DestinationLatitude,
                g.DestinationLongitude,
                g.DestinationAddress,
                g.FromZoneKey,
                g.ToZoneKey,
                g.GroupFareRuleId,
                g.Capacity,
                g.JoinedMemberCount,
                g.Status,
                g.FareAmount,
                g.CommissionRate,
                g.CommissionAmount,
                g.CaptainEarnings,
                g.TotalAmount,
                g.PaymentMethod,
                g.IsCashConfirmed,
                g.MembershipLocked,
                g.DriverId,
                DriverName = g.Driver == null ? null : (g.Driver.User.FullName ?? g.Driver.User.Phone),
                g.ConfirmedAt,
                g.AssignedAt,
                g.StartedAt,
                g.CompletedAt,
                g.CancelledAt,
                g.ReferenceCode,
                g.CreatedAt
            })
            .FirstAsync(ct);

        var members = await _db.GroupMembers
            .AsNoTracking()
            .Where(m => m.GroupRequestId == groupId && !m.IsDeleted)
            .OrderByDescending(m => m.IsOrganizer)
            .ThenBy(m => m.JoinedAt)
            .Select(m => new GroupMemberDto(
                m.UserId,
                m.User.FullName ?? m.User.Phone,
                m.IsOrganizer,
                m.JoinedAt))
            .ToListAsync(ct);

        return new GroupDto(
            row.Id,
            row.OrganizerUserId,
            row.PickupLatitude,
            row.PickupLongitude,
            row.PickupAddress,
            row.DestinationLatitude,
            row.DestinationLongitude,
            row.DestinationAddress,
            row.FromZoneKey,
            row.ToZoneKey,
            row.GroupFareRuleId,
            row.Capacity,
            row.JoinedMemberCount,
            row.Status.ToString(),
            row.FareAmount,
            row.CommissionRate,
            row.CommissionAmount,
            row.CaptainEarnings,
            row.TotalAmount,
            row.PaymentMethod,
            row.IsCashConfirmed,
            row.MembershipLocked,
            row.DriverId,
            row.DriverName,
            row.ConfirmedAt,
            row.AssignedAt,
            row.StartedAt,
            row.CompletedAt,
            row.CancelledAt,
            row.ReferenceCode,
            row.CreatedAt,
            members);
    }

    private static long GroupLifecycleLockKey(Guid groupId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("group-lifecycle|" + groupId.ToString("N")));
        return BitConverter.ToInt64(hash, 0);
    }
}
