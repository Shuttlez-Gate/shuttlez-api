using Shuttlez.Application.Bookings;
using Shuttlez.Domain.Entities;

namespace Shuttlez.Application.Rides;

/// <summary>Server-side distance or legacy flat fare for Ride/Group rules.</summary>
public static class DistanceBasedFareCalculator
{
    public static decimal CalculateRideFare(RideFareRule rule, decimal distanceKm)
    {
        if (rule.PricePerKm > 0)
            return NormalizeDistanceFare(
                rule.BaseFare,
                distanceKm,
                rule.PricePerKm,
                rule.MinimumFare,
                rule.MaximumFare);

        return ShuttleFinancialCalculator.NormalizeMoney(rule.FlatFare);
    }

    public static decimal CalculateGroupFare(GroupFareRule rule, decimal distanceKm)
    {
        if (rule.PricePerKm > 0)
            return NormalizeDistanceFare(
                rule.BaseFare,
                distanceKm,
                rule.PricePerKm,
                rule.MinimumFare,
                rule.MaximumFare);

        return ShuttleFinancialCalculator.NormalizeMoney(rule.CharterFlatFare);
    }

    private static decimal NormalizeDistanceFare(
        decimal baseFare,
        decimal distanceKm,
        decimal pricePerKm,
        decimal? minimumFare,
        decimal? maximumFare)
    {
        var fare = baseFare + (distanceKm * pricePerKm);

        if (minimumFare.HasValue && fare < minimumFare.Value)
            fare = minimumFare.Value;

        if (maximumFare.HasValue && fare > maximumFare.Value)
            fare = maximumFare.Value;

        return ShuttleFinancialCalculator.NormalizeMoney(fare);
    }
}
