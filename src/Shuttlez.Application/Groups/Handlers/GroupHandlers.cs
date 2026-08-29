using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Bookings;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.Groups.DTOs;
using Shuttlez.Application.Rides;
using Shuttlez.Application.RouteMatching.Models;
using Shuttlez.Domain.Entities;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Groups.Handlers;

public record GetGroupQuoteQuery(
    string? FromZoneKey,
    string? ToZoneKey,
    double? PickupLatitude = null,
    double? PickupLongitude = null,
    double? DestinationLatitude = null,
    double? DestinationLongitude = null) : IRequest<GroupQuoteDto>;

public record GetGroupFareOptionsQuery : IRequest<IReadOnlyList<GroupFareOptionDto>>;

public record CreateGroupCommand(CreateGroupRequest Request) : IRequest<GroupDto>;

public record JoinGroupCommand(Guid GroupId) : IRequest<GroupDto>;

public record LeaveGroupCommand(Guid GroupId) : IRequest<GroupDto>;

public record ConfirmGroupCashCommand(Guid GroupId, ConfirmGroupCashRequest Request) : IRequest<GroupDto>;

public record GetMyGroupsQuery : IRequest<IReadOnlyList<GroupDto>>;

public record GetGroupByIdQuery(Guid GroupId) : IRequest<GroupDto>;

public record CancelGroupCommand(Guid GroupId) : IRequest<GroupDto>;

public class GroupHandlers :
    IRequestHandler<GetGroupQuoteQuery, GroupQuoteDto>,
    IRequestHandler<GetGroupFareOptionsQuery, IReadOnlyList<GroupFareOptionDto>>,
    IRequestHandler<CreateGroupCommand, GroupDto>,
    IRequestHandler<JoinGroupCommand, GroupDto>,
    IRequestHandler<LeaveGroupCommand, GroupDto>,
    IRequestHandler<ConfirmGroupCashCommand, GroupDto>,
    IRequestHandler<GetMyGroupsQuery, IReadOnlyList<GroupDto>>,
    IRequestHandler<GetGroupByIdQuery, GroupDto>,
    IRequestHandler<CancelGroupCommand, GroupDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IGroupFareResolver _fareResolver;
    private readonly IShuttleCommissionResolver _commissionResolver;
    private readonly ITripDistanceService _tripDistance;

    public GroupHandlers(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IGroupFareResolver fareResolver,
        IShuttleCommissionResolver commissionResolver,
        ITripDistanceService tripDistance)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _fareResolver = fareResolver;
        _commissionResolver = commissionResolver;
        _tripDistance = tripDistance;
    }

    public async Task<GroupQuoteDto> Handle(
        GetGroupQuoteQuery request,
        CancellationToken cancellationToken)
    {
        var asOf = _clock.UtcNow;
        var rule = await _fareResolver.ResolveAsync(
            request.FromZoneKey, request.ToZoneKey, asOf, cancellationToken);
        var platformPercent = await _commissionResolver.GetPlatformCommissionPercentAsync(
            null, null, asOf, cancellationToken);

        var hasCoords = HasCoordinates(
            request.PickupLatitude,
            request.PickupLongitude,
            request.DestinationLatitude,
            request.DestinationLongitude);

        if (rule.PricePerKm > 0 && !hasCoords)
        {
            throw new AppException(
                "إحداثيات الانطلاق والوجهة مطلوبة لتسعير المجموعة حسب المسافة.",
                400,
                ErrorCodes.TripCoordinatesRequired);
        }

        decimal? distanceKm = null;
        if (hasCoords)
        {
            distanceKm = await _tripDistance.GetDistanceKmAsync(
                new GeoCoordinate(request.PickupLatitude!.Value, request.PickupLongitude!.Value),
                new GeoCoordinate(request.DestinationLatitude!.Value, request.DestinationLongitude!.Value),
                cancellationToken);
        }

        var fare = rule.PricePerKm > 0
            ? DistanceBasedFareCalculator.CalculateGroupFare(rule, distanceKm!.Value)
            : ShuttleFinancialCalculator.NormalizeMoney(rule.CharterFlatFare);

        var (_, commission, captain) =
            ShuttleFinancialCalculator.SplitEarnings(fare, platformPercent);

        return new GroupQuoteDto(
            rule.Id,
            rule.Name,
            rule.FromZoneKey,
            rule.ToZoneKey,
            rule.CharterFlatFare,
            rule.MaxMembers,
            platformPercent,
            commission,
            captain,
            fare,
            CashPaymentPolicy.Cash,
            distanceKm,
            rule.PricePerKm > 0 ? rule.BaseFare : null,
            rule.PricePerKm > 0 ? rule.PricePerKm : null,
            rule.PricePerKm > 0 ? rule.MinimumFare : null);
    }

    public async Task<IReadOnlyList<GroupFareOptionDto>> Handle(
        GetGroupFareOptionsQuery request,
        CancellationToken cancellationToken)
    {
        var asOf = _clock.UtcNow;
        return await _db.GroupFareRules
            .AsNoTracking()
            .Where(r =>
                !r.IsDeleted &&
                r.IsActive &&
                (r.CharterFlatFare > 0 || r.PricePerKm > 0) &&
                r.MaxMembers >= 1 &&
                (r.EffectiveFrom == null || r.EffectiveFrom <= asOf) &&
                (r.EffectiveTo == null || r.EffectiveTo >= asOf))
            .OrderByDescending(r => r.UpdatedAt ?? r.CreatedAt)
            .Select(r => new GroupFareOptionDto(
                r.Id,
                r.Name,
                r.FromZoneKey,
                r.ToZoneKey,
                r.CharterFlatFare,
                r.MaxMembers))
            .ToListAsync(cancellationToken);
    }

    public async Task<GroupDto> Handle(
        CreateGroupCommand request,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var body = request.Request;
        var asOf = _clock.UtcNow;

        var rule = await _fareResolver.ResolveAsync(
            body.FromZoneKey, body.ToZoneKey, asOf, cancellationToken);

        if (body.Capacity < 1 || body.Capacity > rule.MaxMembers)
        {
            throw new AppException(
                $"سعة المجموعة يجب أن تكون بين 1 و {rule.MaxMembers}",
                400,
                ErrorCodes.GroupInvalidCapacity);
        }

        var group = new GroupRequest
        {
            OrganizerUserId = userId,
            PickupLatitude = body.PickupLatitude,
            PickupLongitude = body.PickupLongitude,
            PickupAddress = TrimOrNull(body.PickupAddress),
            DestinationLatitude = body.DestinationLatitude,
            DestinationLongitude = body.DestinationLongitude,
            DestinationAddress = TrimOrNull(body.DestinationAddress),
            FromZoneKey = TrimOrNull(body.FromZoneKey),
            ToZoneKey = TrimOrNull(body.ToZoneKey),
            GroupFareRuleId = rule.Id,
            Capacity = body.Capacity,
            JoinedMemberCount = 1,
            Status = GroupRequestStatus.Draft,
            IsCashConfirmed = false,
            MembershipLocked = false,
            ReferenceCode = $"GR-{asOf:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}"
        };

        _db.Add(group);
        _db.Add(new GroupMember
        {
            GroupRequestId = group.Id,
            UserId = userId,
            IsOrganizer = true,
            JoinedAt = asOf
        });

        await _db.SaveChangesAsync(cancellationToken);
        return await ProjectAsync(group.Id, cancellationToken);
    }

    public async Task<GroupDto> Handle(
        JoinGroupCommand request,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId();

        return await _db.ExecuteInSerializableTransactionAsync(async ct =>
        {
            await _db.AcquireTransactionAdvisoryLockAsync(GroupLockKey(request.GroupId), ct);

            var group = await _db.GroupRequests
                .FirstOrDefaultAsync(g => g.Id == request.GroupId && !g.IsDeleted, ct)
                ?? throw new NotFoundException("المجموعة غير موجودة", ErrorCodes.GroupNotFound);

            if (group.Status != GroupRequestStatus.Draft)
                throw new AppException("لا يمكن الانضمام إلا لمجموعة مسودة", 400, ErrorCodes.GroupNotConfirmable);

            if (group.MembershipLocked)
                throw new AppException("عضوية المجموعة مقفلة", 400, ErrorCodes.GroupMembershipLocked);

            var already = await _db.GroupMembers.AnyAsync(
                m => m.GroupRequestId == group.Id && m.UserId == userId && !m.IsDeleted, ct);

            if (already)
                throw new AppException("أنت عضو في هذه المجموعة بالفعل", 400, ErrorCodes.GroupDuplicateMember);

            if (group.JoinedMemberCount >= group.Capacity)
                throw new AppException("اكتملت سعة المجموعة", 409, ErrorCodes.GroupCapacityExceeded);

            group.JoinedMemberCount += 1;
            group.UpdatedAt = _clock.UtcNow;
            _db.Update(group);
            _db.Add(new GroupMember
            {
                GroupRequestId = group.Id,
                UserId = userId,
                IsOrganizer = false,
                JoinedAt = _clock.UtcNow
            });

            await _db.SaveChangesAsync(ct);
            return await ProjectAsync(group.Id, ct);
        }, cancellationToken);
    }

    public async Task<GroupDto> Handle(
        LeaveGroupCommand request,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId();

        return await _db.ExecuteInSerializableTransactionAsync(async ct =>
        {
            await _db.AcquireTransactionAdvisoryLockAsync(GroupLockKey(request.GroupId), ct);

            var group = await _db.GroupRequests
                .FirstOrDefaultAsync(g => g.Id == request.GroupId && !g.IsDeleted, ct)
                ?? throw new NotFoundException("المجموعة غير موجودة", ErrorCodes.GroupNotFound);

            if (group.Status != GroupRequestStatus.Draft)
                throw new AppException("لا يمكن مغادرة المجموعة إلا وهي مسودة", 400, ErrorCodes.GroupMembershipLocked);

            if (group.MembershipLocked)
                throw new AppException("عضوية المجموعة مقفلة", 400, ErrorCodes.GroupMembershipLocked);

            if (group.OrganizerUserId == userId)
                throw new AppException("المنظم لا يمكنه مغادرة المجموعة", 400, ErrorCodes.GroupNotMember);

            var member = await _db.GroupMembers
                .FirstOrDefaultAsync(
                    m => m.GroupRequestId == group.Id && m.UserId == userId && !m.IsDeleted,
                    ct)
                ?? throw new AppException("لست عضواً في هذه المجموعة", 400, ErrorCodes.GroupNotMember);

            member.IsDeleted = true;
            member.UpdatedAt = _clock.UtcNow;
            _db.Update(member);

            if (group.JoinedMemberCount > 0)
                group.JoinedMemberCount -= 1;

            group.UpdatedAt = _clock.UtcNow;
            _db.Update(group);
            await _db.SaveChangesAsync(ct);
            return await ProjectAsync(group.Id, ct);
        }, cancellationToken);
    }

    public async Task<GroupDto> Handle(
        ConfirmGroupCashCommand request,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var paymentMethod = RequireCashOnly(request.Request.PaymentMethod);

        return await _db.ExecuteInSerializableTransactionAsync(async ct =>
        {
            await _db.AcquireTransactionAdvisoryLockAsync(GroupLockKey(request.GroupId), ct);

            var group = await _db.GroupRequests
                .FirstOrDefaultAsync(g => g.Id == request.GroupId && !g.IsDeleted, ct)
                ?? throw new NotFoundException("المجموعة غير موجودة", ErrorCodes.GroupNotFound);

            if (group.OrganizerUserId != userId && !_currentUser.IsAdmin)
                throw new ForbiddenAppException("تأكيد الدفع متاح للمنظم فقط");

            if (group.Status != GroupRequestStatus.Draft)
            {
                throw new AppException(
                    "لا يمكن تأكيد المجموعة في حالتها الحالية",
                    400,
                    ErrorCodes.GroupNotConfirmable);
            }

            var asOf = _clock.UtcNow;
            var rule = await _fareResolver.ResolveAsync(
                group.FromZoneKey, group.ToZoneKey, asOf, ct);

            if (group.GroupFareRuleId is null)
                group.GroupFareRuleId = rule.Id;

            var platformPercent = await _commissionResolver.GetPlatformCommissionPercentAsync(
                null, null, asOf, ct);

            var distanceKm = await _tripDistance.GetDistanceKmAsync(
                new GeoCoordinate(group.PickupLatitude, group.PickupLongitude),
                new GeoCoordinate(group.DestinationLatitude, group.DestinationLongitude),
                ct);

            var fare = rule.PricePerKm > 0
                ? DistanceBasedFareCalculator.CalculateGroupFare(rule, distanceKm)
                : ShuttleFinancialCalculator.NormalizeMoney(rule.CharterFlatFare);

            var (rate, commission, captain) =
                ShuttleFinancialCalculator.SplitEarnings(fare, platformPercent);

            group.DistanceKm = distanceKm;
            group.BaseFareApplied = rule.PricePerKm > 0 ? rule.BaseFare : null;
            group.PricePerKmApplied = rule.PricePerKm > 0 ? rule.PricePerKm : null;
            group.MinimumFareApplied = rule.PricePerKm > 0 ? rule.MinimumFare : null;
            group.FareAmount = fare;
            group.CommissionRate = rate;
            group.CommissionAmount = commission;
            group.CaptainEarnings = captain;
            group.TotalAmount = fare;
            group.PaymentMethod = paymentMethod;
            group.IsCashConfirmed = true;
            group.MembershipLocked = true;
            group.Status = GroupRequestStatus.Confirmed;
            group.ConfirmedAt = asOf;
            group.UpdatedAt = asOf;
            _db.Update(group);
            await _db.SaveChangesAsync(ct);
            return await ProjectAsync(group.Id, ct);
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<GroupDto>> Handle(
        GetMyGroupsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var ids = await _db.GroupRequests
            .AsNoTracking()
            .Where(g =>
                !g.IsDeleted &&
                (g.OrganizerUserId == userId ||
                 _db.GroupMembers.Any(m =>
                     m.GroupRequestId == g.Id && m.UserId == userId && !m.IsDeleted)))
            .OrderByDescending(g => g.CreatedAt)
            .Select(g => g.Id)
            .ToListAsync(cancellationToken);

        var list = new List<GroupDto>(ids.Count);
        foreach (var id in ids)
            list.Add(await ProjectAsync(id, cancellationToken));
        return list;
    }

    public async Task<GroupDto> Handle(
        GetGroupByIdQuery request,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var group = await _db.GroupRequests
            .AsNoTracking()
            .Where(g => g.Id == request.GroupId && !g.IsDeleted)
            .Select(g => new { g.Id, g.OrganizerUserId, g.DriverId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("المجموعة غير موجودة", ErrorCodes.GroupNotFound);

        if (!_currentUser.IsAdmin && group.OrganizerUserId != userId)
        {
            var isMember = await _db.GroupMembers.AnyAsync(
                m => m.GroupRequestId == group.Id && m.UserId == userId && !m.IsDeleted,
                cancellationToken);

            var isAssignedDriver = group.DriverId is not null && await _db.Drivers
                .AsNoTracking()
                .AnyAsync(
                    d => d.Id == group.DriverId && d.UserId == userId && !d.IsDeleted,
                    cancellationToken);

            if (!isMember && !isAssignedDriver)
                throw new ForbiddenAppException("غير مصرح بعرض هذه المجموعة");
        }

        return await ProjectAsync(group.Id, cancellationToken);
    }

    public async Task<GroupDto> Handle(
        CancelGroupCommand request,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var now = _clock.UtcNow;

        var group = await _db.GroupRequests
            .FirstOrDefaultAsync(g => g.Id == request.GroupId && !g.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("المجموعة غير موجودة", ErrorCodes.GroupNotFound);

        if (!_currentUser.IsAdmin && group.OrganizerUserId != userId)
            throw new ForbiddenAppException("غير مصرح بإلغاء هذه المجموعة");

        if (group.Status == GroupRequestStatus.Cancelled)
            return await ProjectAsync(group.Id, cancellationToken);

        if (group.Status is not (GroupRequestStatus.Draft or GroupRequestStatus.Confirmed))
        {
            throw new AppException(
                "لا يمكن إلغاء المجموعة في حالتها الحالية",
                400,
                ErrorCodes.GroupNotCancellable);
        }

        group.Status = GroupRequestStatus.Cancelled;
        group.CancelledAt ??= now;
        group.UpdatedAt = now;
        _db.Update(group);
        await _db.SaveChangesAsync(cancellationToken);
        return await ProjectAsync(group.Id, cancellationToken);
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
                g.CreatedAt,
                g.DistanceKm
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
            members,
            row.DistanceKm);
    }

    private Guid RequireUserId() =>
        _currentUser.UserId ?? throw new UnauthorizedAppException("غير مصرح");

    private static string RequireCashOnly(string? raw)
    {
        var method = CashPaymentPolicy.NormalizeOrThrow(raw);
        if (!CashPaymentPolicy.IsCash(method))
        {
            throw new AppException(
                "المجموعة تقبل الدفع نقدًا للكابتن فقط.",
                400,
                ErrorCodes.PaymentMethodNotSupported);
        }

        return method;
    }

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool HasCoordinates(
        double? pickupLatitude,
        double? pickupLongitude,
        double? destinationLatitude,
        double? destinationLongitude) =>
        pickupLatitude.HasValue &&
        pickupLongitude.HasValue &&
        destinationLatitude.HasValue &&
        destinationLongitude.HasValue;

    private static long GroupLockKey(Guid groupId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("group-member|" + groupId.ToString("N")));
        return BitConverter.ToInt64(hash, 0);
    }
}
