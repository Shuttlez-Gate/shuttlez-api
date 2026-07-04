using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.Subscriptions.DTOs;
using Shuttlez.Domain.Entities;

namespace Shuttlez.Application.Subscriptions.Queries;

public record GetSubscriptionPackagesQuery : IRequest<IReadOnlyList<SubscriptionPackageDto>>;

public record SubscribePackageCommand(Guid PackageId) : IRequest<SubscribePackageResponse>;

public class SubscriptionHandlers :
    IRequestHandler<GetSubscriptionPackagesQuery, IReadOnlyList<SubscriptionPackageDto>>,
    IRequestHandler<SubscribePackageCommand, SubscribePackageResponse>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SubscriptionHandlers(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<SubscriptionPackageDto>> Handle(
        GetSubscriptionPackagesQuery request,
        CancellationToken cancellationToken)
    {
        var packages = await _db.SubscriptionPackages
            .Where(p => p.IsActive && !p.IsDeleted)
            .OrderBy(p => p.Price)
            .ToListAsync(cancellationToken);

        return packages.Select(MapPackage).ToList();
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

        _db.Add(new Notification
        {
            UserId = userId,
            Title = $"تم الاشتراك في {package.Name}",
            Body = $"تم تفعيل {package.Description ?? package.Name} بنجاح.",
            Type = "subscription",
            IsRead = false
        });

        await _db.SaveChangesAsync(cancellationToken);

        return new SubscribePackageResponse(
            package.Id,
            "تم الاشتراك في الباقة بنجاح");
    }

    private static SubscriptionPackageDto MapPackage(SubscriptionPackage package)
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
            package.ValidityDays);
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
