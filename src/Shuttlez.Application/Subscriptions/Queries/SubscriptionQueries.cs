using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.Subscriptions.DTOs;
using Shuttlez.Domain.Entities;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Subscriptions.Queries;

public record GetSubscriptionPackagesQuery : IRequest<IReadOnlyList<SubscriptionPackageDto>>;

public record SubscribePackageCommand(Guid PackageId) : IRequest<SubscribePackageResponse>;

public record GetMySubscriptionQuery : IRequest<MySubscriptionDto>;

public class SubscriptionHandlers :
    IRequestHandler<GetSubscriptionPackagesQuery, IReadOnlyList<SubscriptionPackageDto>>,
    IRequestHandler<SubscribePackageCommand, SubscribePackageResponse>,
    IRequestHandler<GetMySubscriptionQuery, MySubscriptionDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public SubscriptionHandlers(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<IReadOnlyList<SubscriptionPackageDto>> Handle(
        GetSubscriptionPackagesQuery request,
        CancellationToken cancellationToken)
    {
        var packages = await _db.SubscriptionPackages
            .Where(p => p.IsActive && !p.IsDeleted)
            .OrderBy(p => p.Price)
            .ToListAsync(cancellationToken);

        User? user = null;
        if (_currentUser.UserId is Guid userId)
        {
            user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);
        }

        var usage = user is null
            ? (Used: 0, Remaining: (int?)null, Expires: (DateTime?)null)
            : await ComputeUsageAsync(user, cancellationToken);

        return packages.Select(p =>
        {
            var isCurrent = user?.ActiveSubscriptionPackageId == p.Id;
            return MapPackage(
                p,
                isCurrent,
                isCurrent ? usage.Used : null,
                isCurrent ? usage.Remaining : null,
                isCurrent ? usage.Expires : null);
        }).ToList();
    }

    public async Task<SubscribePackageResponse> Handle(
        SubscribePackageCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAppException("غير مصرح");

        var package = await _db.SubscriptionPackages
            .FirstOrDefaultAsync(
                p => p.Id == request.PackageId && p.IsActive && !p.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException("الباقة غير متاحة");

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken)
            ?? throw new UnauthorizedAppException("غير مصرح");

        var now = _clock.UtcNow;
        user.ActiveSubscriptionPackageId = package.Id;
        user.SubscriptionActivatedAt = now;
        user.SubscriptionExpiresAt = now.AddDays(package.ValidityDays);
        _db.Update(user);

        _db.Add(new Notification
        {
            UserId = userId,
            Title = $"تم الاشتراك في {package.Name}",
            Body = $"تم تفعيل {package.Description ?? package.Name} بنجاح.",
            Type = "subscription",
            IsRead = false
        });

        await _db.SaveChangesAsync(cancellationToken);

        var remaining = package.TripCount <= 0 ? (int?)null : package.TripCount;
        return new SubscribePackageResponse(
            package.Id,
            "تم الاشتراك في الباقة بنجاح",
            package.TripCount,
            0,
            remaining,
            user.SubscriptionExpiresAt);
    }

    public async Task<MySubscriptionDto> Handle(
        GetMySubscriptionQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAppException("غير مصرح");

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken)
            ?? throw new UnauthorizedAppException("غير مصرح");

        if (user.ActiveSubscriptionPackageId is null)
        {
            return new MySubscriptionDto(null, null, 0, 0, null, null, false);
        }

        var package = await _db.SubscriptionPackages
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Id == user.ActiveSubscriptionPackageId,
                cancellationToken);

        var usage = await ComputeUsageAsync(user, cancellationToken);
        var active = user.SubscriptionExpiresAt is not null &&
                     user.SubscriptionExpiresAt >= _clock.UtcNow;

        return new MySubscriptionDto(
            package?.Id,
            package?.Name,
            package?.TripCount ?? 0,
            usage.Used,
            usage.Remaining,
            usage.Expires,
            active);
    }

    private async Task<(int Used, int? Remaining, DateTime? Expires)> ComputeUsageAsync(
        User user,
        CancellationToken cancellationToken)
    {
        if (user.ActiveSubscriptionPackageId is null || user.SubscriptionExpiresAt is null)
            return (0, null, null);

        var package = await _db.SubscriptionPackages
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Id == user.ActiveSubscriptionPackageId,
                cancellationToken);

        if (package is null)
            return (0, null, user.SubscriptionExpiresAt);

        var activated = user.SubscriptionActivatedAt
            ?? user.SubscriptionExpiresAt.Value.AddDays(-Math.Max(package.ValidityDays, 1));

        var used = await _db.Bookings
            .Where(b =>
                b.UserId == user.Id &&
                b.UsesSubscriptionCredit &&
                b.Status == BookingStatus.Confirmed &&
                !b.IsDeleted &&
                b.CreatedAt >= activated &&
                b.CreatedAt <= user.SubscriptionExpiresAt)
            .SumAsync(b => (int?)b.SeatCount, cancellationToken) ?? 0;

        int? remaining = package.TripCount <= 0
            ? null
            : Math.Max(0, package.TripCount - used);

        return (used, remaining, user.SubscriptionExpiresAt);
    }

    private static SubscriptionPackageDto MapPackage(
        SubscriptionPackage package,
        bool isCurrent,
        int? used,
        int? remaining,
        DateTime? expiresAt)
    {
        var oldPrice = package.Description?.Contains("oldPrice:") == true
            ? ParseOldPrice(package.Description)
            : Math.Round(package.Price / 0.8m, 0);

        var duration = package.Description?.Contains(';') == true
            ? package.Description.Split(';')[0].Trim()
            : package.Description?.StartsWith("لمدة") == true
                ? package.Description
                : $"لمدة {package.ValidityDays} يوم";

        return new SubscriptionPackageDto(
            package.Id,
            package.Name,
            duration,
            package.Price,
            oldPrice,
            package.TripCount,
            package.ValidityDays,
            isCurrent,
            used,
            remaining,
            expiresAt);
    }

    private static decimal ParseOldPrice(string description)
    {
        var marker = "oldPrice:";
        var index = description.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return 0;
        var value = description[(index + marker.Length)..].Split(';', ' ')[0];
        return decimal.TryParse(value, out var parsed) ? parsed : 0;
    }
}
