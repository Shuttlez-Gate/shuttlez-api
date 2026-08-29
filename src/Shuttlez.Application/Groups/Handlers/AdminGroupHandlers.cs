using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.Groups.DTOs;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Groups.Handlers;

public record AdminGroupsQuery(
    string? Status = null,
    Guid? DriverId = null,
    Guid? OrganizerUserId = null,
    int? Page = null,
    int? PageSize = null) : IRequest<PagedResult<GroupDto>>;

public record AssignGroupDriverCommand(Guid GroupId, Guid DriverId) : IRequest<GroupDto>;

public record UnassignGroupDriverCommand(Guid GroupId) : IRequest<GroupDto>;

public class AdminGroupHandlers :
    IRequestHandler<AdminGroupsQuery, PagedResult<GroupDto>>,
    IRequestHandler<AssignGroupDriverCommand, GroupDto>,
    IRequestHandler<UnassignGroupDriverCommand, GroupDto>
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public AdminGroupHandlers(IAppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<PagedResult<GroupDto>> Handle(
        AdminGroupsQuery request,
        CancellationToken cancellationToken)
    {
        var page = PageRequest.From(request.Page, request.PageSize);
        var query = _db.GroupRequests.AsNoTracking().Where(g => !g.IsDeleted);

        if (request.DriverId is not null)
            query = query.Where(g => g.DriverId == request.DriverId);

        if (request.OrganizerUserId is not null)
            query = query.Where(g => g.OrganizerUserId == request.OrganizerUserId);

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<GroupRequestStatus>(request.Status, true, out var status))
        {
            query = query.Where(g => g.Status == status);
        }

        var total = await query.CountAsync(cancellationToken);
        var ids = await query
            .OrderByDescending(g => g.CreatedAt)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(g => g.Id)
            .ToListAsync(cancellationToken);

        var items = new List<GroupDto>(ids.Count);
        foreach (var id in ids)
            items.Add(await ProjectAsync(id, cancellationToken));

        return new PagedResult<GroupDto>(items, page.Page, page.PageSize, total);
    }

    public async Task<GroupDto> Handle(
        AssignGroupDriverCommand request,
        CancellationToken cancellationToken)
    {
        return await _db.ExecuteInSerializableTransactionAsync(async ct =>
        {
            await _db.AcquireTransactionAdvisoryLockAsync(GroupAssignLockKey(request.GroupId), ct);

            var group = await _db.GroupRequests
                .FirstOrDefaultAsync(g => g.Id == request.GroupId && !g.IsDeleted, ct)
                ?? throw new NotFoundException("المجموعة غير موجودة", ErrorCodes.GroupNotFound);

            if (group.Status == GroupRequestStatus.Cancelled)
                throw new AppException("لا يمكن تعيين كابتن لمجموعة ملغاة", 400, ErrorCodes.GroupNotAssignable);

            if (group.Status == GroupRequestStatus.Completed)
                throw new AppException("لا يمكن تعيين كابتن لمجموعة مكتملة", 400, ErrorCodes.GroupNotAssignable);

            if (group.Status == GroupRequestStatus.InProgress)
                throw new AppException("لا يمكن إعادة تعيين كابتن أثناء تنفيذ المجموعة", 400, ErrorCodes.GroupNotAssignable);

            if (group.Status is not (GroupRequestStatus.Confirmed or GroupRequestStatus.Assigned))
            {
                throw new AppException(
                    "يجب تأكيد المجموعة نقدًا قبل تعيين الكابتن",
                    400,
                    ErrorCodes.GroupNotAssignable);
            }

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

            var groupConflict = await _db.GroupRequests.AnyAsync(
                g => g.DriverId == driver.Id &&
                     g.Id != group.Id &&
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

            var rideConflict = await _db.RideRequests.AnyAsync(
                r => r.DriverId == driver.Id &&
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

            var now = _clock.UtcNow;
            group.DriverId = driver.Id;
            group.Status = GroupRequestStatus.Assigned;
            group.AssignedAt ??= now;
            group.UpdatedAt = now;
            _db.Update(group);
            await _db.SaveChangesAsync(ct);
            return await ProjectAsync(group.Id, ct);
        }, cancellationToken);
    }

    public async Task<GroupDto> Handle(
        UnassignGroupDriverCommand request,
        CancellationToken cancellationToken)
    {
        return await _db.ExecuteInSerializableTransactionAsync(async ct =>
        {
            await _db.AcquireTransactionAdvisoryLockAsync(GroupAssignLockKey(request.GroupId), ct);

            var group = await _db.GroupRequests
                .FirstOrDefaultAsync(g => g.Id == request.GroupId && !g.IsDeleted, ct)
                ?? throw new NotFoundException("المجموعة غير موجودة", ErrorCodes.GroupNotFound);

            if (group.Status == GroupRequestStatus.Cancelled)
                throw new AppException("المجموعة ملغاة", 400, ErrorCodes.GroupNotCancellable);

            if (group.Status == GroupRequestStatus.Completed)
                throw new AppException("المجموعة مكتملة", 400, ErrorCodes.GroupNotCompletable);

            if (group.Status == GroupRequestStatus.InProgress)
                throw new AppException("لا يمكن إلغاء التعيين أثناء تنفيذ المجموعة", 400, ErrorCodes.GroupNotStartable);

            group.DriverId = null;
            group.AssignedAt = null;
            if (group.Status == GroupRequestStatus.Assigned)
                group.Status = GroupRequestStatus.Confirmed;

            group.UpdatedAt = _clock.UtcNow;
            _db.Update(group);
            await _db.SaveChangesAsync(ct);
            return await ProjectAsync(group.Id, ct);
        }, cancellationToken);
    }

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

    private static long GroupAssignLockKey(Guid groupId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("group-assign|" + groupId.ToString("N")));
        return BitConverter.ToInt64(hash, 0);
    }
}
