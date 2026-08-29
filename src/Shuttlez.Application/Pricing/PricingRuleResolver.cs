using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shuttlez.Application.Bookings;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.Landing;
using Shuttlez.Domain.Entities;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Pricing;

public interface IPricingRuleResolver
{
    Task<PricingRule?> FindActiveRuleAsync(
        Guid? routeId,
        VehicleType vehicleType,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default);

    Task<decimal> ResolveOneWayPriceAsync(
        Guid? routeId,
        VehicleType vehicleType,
        DateTime asOfUtc,
        decimal? fallbackPrice,
        CancellationToken cancellationToken = default);

    /// Platform commission % preferring PricingRule launch/permanent window, else global CommissionRule.
    Task<decimal> ResolvePlatformCommissionPercentAsync(
        Guid? routeId,
        VehicleType? vehicleType,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default);
}

public class PricingRuleResolver : IPricingRuleResolver
{
    private readonly IAppDbContext _db;
    private readonly ShuttleCommissionSettings _settings;
    private readonly CaptainLaunchOfferSettings _launchOffer;

    public PricingRuleResolver(
        IAppDbContext db,
        IOptions<ShuttleCommissionSettings> settings,
        IOptions<CaptainLaunchOfferSettings> launchOffer)
    {
        _db = db;
        _settings = settings.Value;
        _launchOffer = launchOffer.Value;
    }

    public async Task<PricingRule?> FindActiveRuleAsync(
        Guid? routeId,
        VehicleType vehicleType,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default)
    {
        var q = _db.PricingRules
            .AsNoTracking()
            .Where(r =>
                !r.IsDeleted &&
                r.IsActive &&
                r.VehicleType == vehicleType &&
                (r.EffectiveFrom == null || r.EffectiveFrom <= asOfUtc) &&
                (r.EffectiveTo == null || r.EffectiveTo >= asOfUtc));

        if (routeId is Guid rid)
        {
            var routeSpecific = await q
                .Where(r => r.RouteId == rid)
                .OrderByDescending(r => r.EffectiveFrom)
                .FirstOrDefaultAsync(cancellationToken);
            if (routeSpecific is not null)
                return routeSpecific;
        }

        return await q
            .Where(r => r.RouteId == null)
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<decimal> ResolveOneWayPriceAsync(
        Guid? routeId,
        VehicleType vehicleType,
        DateTime asOfUtc,
        decimal? fallbackPrice,
        CancellationToken cancellationToken = default)
    {
        var rule = await FindActiveRuleAsync(routeId, vehicleType, asOfUtc, cancellationToken);
        if (rule is not null)
            return ShuttlePricingCalculator.NormalizeMoney(rule.OneWayPrice);
        if (fallbackPrice is decimal f && f >= 0)
            return ShuttlePricingCalculator.NormalizeMoney(f);
        throw new InvalidOperationException("PRICING_NOT_AVAILABLE");
    }

    public async Task<decimal> ResolvePlatformCommissionPercentAsync(
        Guid? routeId,
        VehicleType? vehicleType,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default)
    {
        if (vehicleType is VehicleType vt)
        {
            var pricing = await FindActiveRuleAsync(routeId, vt, asOfUtc, cancellationToken);
            if (pricing is not null)
            {
                var launchStart = ShuttlePricingCalculator.ResolveLaunchStart(
                    pricing.LaunchStartAt,
                    pricing.EffectiveFrom,
                    pricing.CreatedAt);
                return ShuttlePricingCalculator.ResolveCommissionPercent(
                    pricing.LaunchCommissionPercent,
                    pricing.PermanentCommissionPercent,
                    pricing.LaunchPeriodDays,
                    launchStart,
                    asOfUtc);
            }
        }

        var rule = await _db.CommissionRules
            .AsNoTracking()
            .Where(r =>
                r.IsActive &&
                !r.IsDeleted &&
                (r.EffectiveFrom == null || r.EffectiveFrom <= asOfUtc) &&
                (r.EffectiveTo == null || r.EffectiveTo >= asOfUtc))
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

        if (rule is not null)
            return Math.Clamp(rule.PlatformCommissionPercent, 0m, 100m);

        if (_settings.PreferLaunchOfferPercent)
        {
            var captainShare = Math.Clamp(_launchOffer.ProfitPercent, 0, 100);
            return Math.Clamp(100m - captainShare, 0m, 100m);
        }

        return Math.Clamp(_settings.PlatformCommissionPercent, 0m, 100m);
    }
}
