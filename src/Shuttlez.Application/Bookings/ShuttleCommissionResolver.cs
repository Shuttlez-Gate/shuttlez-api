using Shuttlez.Application.Pricing;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Bookings;

public interface IShuttleCommissionResolver
{
    Task<decimal> GetPlatformCommissionPercentAsync(CancellationToken cancellationToken = default);

    Task<decimal> GetPlatformCommissionPercentAsync(
        Guid? routeId,
        VehicleType? vehicleType,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default);
}

/// Delegates to <see cref="IPricingRuleResolver"/> (PricingRule launch window, then global CommissionRule).
public class ShuttleCommissionResolver : IShuttleCommissionResolver
{
    private readonly IPricingRuleResolver _pricing;

    public ShuttleCommissionResolver(IPricingRuleResolver pricing) => _pricing = pricing;

    public Task<decimal> GetPlatformCommissionPercentAsync(
        CancellationToken cancellationToken = default) =>
        GetPlatformCommissionPercentAsync(null, null, DateTime.UtcNow, cancellationToken);

    public Task<decimal> GetPlatformCommissionPercentAsync(
        Guid? routeId,
        VehicleType? vehicleType,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default) =>
        _pricing.ResolvePlatformCommissionPercentAsync(
            routeId,
            vehicleType,
            asOfUtc,
            cancellationToken);
}
