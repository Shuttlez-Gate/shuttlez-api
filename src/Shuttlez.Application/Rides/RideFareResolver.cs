using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Domain.Entities;

namespace Shuttlez.Application.Rides;

public interface IRideFareResolver
{
    Task<RideFareRule> ResolveAsync(
        string? fromZoneKey,
        string? toZoneKey,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default);
}

public class RideFareResolver : IRideFareResolver
{
    private readonly IAppDbContext _db;

    public RideFareResolver(IAppDbContext db) => _db = db;

    public async Task<RideFareRule> ResolveAsync(
        string? fromZoneKey,
        string? toZoneKey,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default)
    {
        var from = NormalizeZone(fromZoneKey);
        var to = NormalizeZone(toZoneKey);

        var active = await _db.RideFareRules
            .Where(r =>
                !r.IsDeleted &&
                r.IsActive &&
                (r.FlatFare > 0 || r.PricePerKm > 0) &&
                (r.EffectiveFrom == null || r.EffectiveFrom <= asOfUtc) &&
                (r.EffectiveTo == null || r.EffectiveTo >= asOfUtc))
            .OrderByDescending(r => r.UpdatedAt ?? r.CreatedAt)
            .ToListAsync(cancellationToken);

        if (from is not null && to is not null)
        {
            var exact = active.FirstOrDefault(r =>
                string.Equals(NormalizeZone(r.FromZoneKey), from, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(NormalizeZone(r.ToZoneKey), to, StringComparison.OrdinalIgnoreCase));
            if (exact is not null)
                return exact;

            var reverse = active.FirstOrDefault(r =>
                string.Equals(NormalizeZone(r.FromZoneKey), to, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(NormalizeZone(r.ToZoneKey), from, StringComparison.OrdinalIgnoreCase));
            if (reverse is not null)
                return reverse;
        }

        var flat = active.FirstOrDefault(r =>
            string.IsNullOrWhiteSpace(r.FromZoneKey) &&
            string.IsNullOrWhiteSpace(r.ToZoneKey));

        if (flat is not null)
            return flat;

        throw new AppException(
            "لا توجد قاعدة تسعير نشطة للمشوار. يرجى ضبط كتالوج أجرة العربية من لوحة التحكم.",
            400,
            ErrorCodes.RideFareNotConfigured);
    }

    private static string? NormalizeZone(string? key) =>
        string.IsNullOrWhiteSpace(key) ? null : key.Trim().ToLowerInvariant();
}
