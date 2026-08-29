using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.Pricing;
using Shuttlez.Domain.Entities;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Admin.Services;

public interface IRouteDemandReadinessEnricher
{
    Task<IReadOnlyDictionary<string, RouteDemandReadinessResult>> EnrichAsync(
        IReadOnlyList<RouteDemandReadinessSource> sources,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default);
}

/// <summary>Minimal corridor facts needed to resolve readiness (no fake demand).</summary>
public sealed record RouteDemandReadinessSource(
    string RouteKey,
    string EndpointA,
    string EndpointB,
    int DemandCount,
    int ConfirmedPassengers,
    int UniquePassengers,
    string? RecommendedVehicleCode,
    string? AssignedVehicleType,
    IReadOnlyList<string> PreferredVehicleTypes,
    /// <summary>Admin-persisted catalog RouteId. Takes priority over exact key match.</summary>
    Guid? ExplicitMappedRouteId = null);

/// <summary>
/// Batches Routes + PricingRules + fleet capacities.
/// Safe Route match only (exact bidirectional key from Route.Name). No fuzzy text.
/// Pricing is resolved only when RouteId is safely linked.
/// Capacity from fleet / default hints — never marketing 3/33.
/// </summary>
public sealed class RouteDemandReadinessEnricher : IRouteDemandReadinessEnricher
{
    private static readonly Dictionary<VehicleType, int> DefaultCapacityHints = new()
    {
        [VehicleType.CarShuttle] = 4,
        [VehicleType.MiniBus] = 13,
        [VehicleType.Bus] = 24,
    };

    private readonly IAppDbContext _db;

    public RouteDemandReadinessEnricher(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyDictionary<string, RouteDemandReadinessResult>> EnrichAsync(
        IReadOnlyList<RouteDemandReadinessSource> sources,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default)
    {
        if (sources.Count == 0)
            return new Dictionary<string, RouteDemandReadinessResult>();

        var routes = await _db.Routes
            .AsNoTracking()
            .Where(r => !r.IsDeleted && r.IsActive)
            .Select(r => new { r.Id, r.Name })
            .ToListAsync(cancellationToken);

        var routeByKey = BuildRouteKeyIndex(routes.Select(r => (r.Id, r.Name)));

        var fleetCaps = await _db.Vehicles
            .AsNoTracking()
            .Where(v => !v.IsDeleted && v.IsActive && v.Capacity > 0)
            .GroupBy(v => v.Type)
            .Select(g => new { Type = g.Key, Cap = g.Max(v => v.Capacity) })
            .ToListAsync(cancellationToken);
        var capacityByType = fleetCaps.ToDictionary(x => x.Type, x => x.Cap);

        // Prefer empty pricing over hard failure when PricingRulesSet is not migrated yet.
        // Readiness then surfaces NO_PRICING / MISSING_ROUTE_LINK accurately — never invents fares.
        List<PricingRule> pricingRules;
        try
        {
            pricingRules = await _db.PricingRules
                .AsNoTracking()
                .Where(r =>
                    !r.IsDeleted &&
                    r.IsActive &&
                    (r.EffectiveFrom == null || r.EffectiveFrom <= asOfUtc) &&
                    (r.EffectiveTo == null || r.EffectiveTo >= asOfUtc))
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex) when (IsMissingRelation(ex, "PricingRulesSet"))
        {
            pricingRules = [];
        }

        var result = new Dictionary<string, RouteDemandReadinessResult>(StringComparer.Ordinal);

        foreach (var src in sources)
        {
            var (routeId, routeSafe, ambiguous, linkSource) = ResolveRouteLink(
                src.ExplicitMappedRouteId,
                src.RouteKey,
                routes.Select(r => r.Id).ToHashSet(),
                routeByKey);

            var (vehicleType, _) = ResolveVehicleType(src);
            int? capacity = null;
            if (vehicleType is VehicleType vt)
            {
                if (capacityByType.TryGetValue(vt, out var fleetCap) && fleetCap > 0)
                {
                    capacity = fleetCap;
                }
                else if (DefaultCapacityHints.TryGetValue(vt, out var hint))
                {
                    capacity = hint;
                }
            }

            // Strict: only resolve PricingRule when RouteId is safely linked.
            PricingRule? rule = null;
            string? pricingSource = null;
            decimal? commissionPct = null;
            string? commissionType = null;
            DateTime? launchStart = null;
            DateTime? launchEnd = null;
            bool? launchActive = null;
            int? launchDays = null;

            if (routeSafe && routeId is Guid rid && vehicleType is VehicleType vehicle)
            {
                rule = PickPricingRule(pricingRules, rid, vehicle);
                if (rule is not null)
                {
                    pricingSource = rule.RouteId == rid ? "ROUTE_VEHICLE" : "VEHICLE_DEFAULT";
                    launchStart = ShuttlePricingCalculator.ResolveLaunchStart(
                        rule.LaunchStartAt,
                        rule.EffectiveFrom,
                        rule.CreatedAt);
                    launchDays = rule.LaunchPeriodDays;
                    launchEnd = launchDays > 0 ? launchStart.Value.AddDays(launchDays.Value) : null;
                    commissionPct = ShuttlePricingCalculator.ResolveCommissionPercent(
                        rule.LaunchCommissionPercent,
                        rule.PermanentCommissionPercent,
                        rule.LaunchPeriodDays,
                        launchStart.Value,
                        asOfUtc);
                    launchActive = launchDays > 0 && asOfUtc < launchEnd;
                    commissionType = launchActive == true ? "LAUNCH" : "PERMANENT";
                }
            }

            var input = new RouteDemandReadinessInput(
                src.DemandCount,
                src.ConfirmedPassengers,
                src.UniquePassengers,
                routeId,
                routeSafe,
                vehicleType,
                capacity,
                rule?.Id,
                pricingSource,
                rule?.OneWayPrice,
                rule?.RoundTripPrice,
                rule?.WeeklyPrice,
                rule?.MonthlyPrice,
                rule?.MinimumLaunchRiders,
                rule?.TargetOccupancy,
                commissionPct,
                commissionType,
                launchDays,
                launchStart,
                launchEnd,
                launchActive,
                ambiguous);

            var evaluated = RouteDemandReadinessCalculator.Evaluate(input);
            result[src.RouteKey] = evaluated with { RouteLinkSource = linkSource };
        }

        return result;
    }

    /// <summary>
    /// Explicit Admin mapping wins. Otherwise exact bidirectional RouteKey only.
    /// No fuzzy matching.
    /// </summary>
    public static (Guid? RouteId, bool RouteSafe, bool Ambiguous, string? LinkSource) ResolveRouteLink(
        Guid? explicitMappedRouteId,
        string routeKey,
        IReadOnlySet<Guid> activeRouteIds,
        IReadOnlyDictionary<string, List<Guid>> routeByKey)
    {
        if (explicitMappedRouteId is Guid mappedId)
        {
            if (activeRouteIds.Contains(mappedId))
                return (mappedId, true, false, "EXPLICIT");

            // Stale mapping — do not fall back to fuzzy; surface missing link.
            return (null, false, false, null);
        }

        if (routeByKey.TryGetValue(routeKey, out var matches))
        {
            if (matches.Count == 1)
                return (matches[0], true, false, "EXACT_KEY");
            if (matches.Count > 1)
                return (null, false, true, null);
        }

        return (null, false, false, null);
    }

    /// <summary>
    /// True when demand RouteKey equals the exact bidirectional key derived from Route.Name.
    /// No fuzzy / partial / governorate-stripping match.
    /// </summary>
    public static bool IsExactBidirectionalMatch(string demandRouteKey, string routeName)
    {
        if (string.IsNullOrWhiteSpace(demandRouteKey) || string.IsNullOrWhiteSpace(routeName))
            return false;

        foreach (var key in DeriveRouteKeys(routeName))
        {
            if (string.Equals(key, demandRouteKey, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    /// <summary>Same priority as PricingRuleResolver: route+vehicle then vehicle default.</summary>
    public static PricingRule? PickPricingRule(
        IReadOnlyList<PricingRule> rules,
        Guid routeId,
        VehicleType vehicleType)
    {
        var forVehicle = rules.Where(r => r.VehicleType == vehicleType);
        var routeSpecific = forVehicle
            .Where(r => r.RouteId == routeId)
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefault();
        if (routeSpecific is not null)
            return routeSpecific;

        return forVehicle
            .Where(r => r.RouteId == null)
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefault();
    }

    public static Dictionary<string, List<Guid>> BuildRouteKeyIndex(
        IEnumerable<(Guid Id, string Name)> routes)
    {
        var map = new Dictionary<string, List<Guid>>(StringComparer.Ordinal);
        foreach (var (id, name) in routes)
        {
            foreach (var key in DeriveRouteKeys(name))
            {
                if (string.IsNullOrWhiteSpace(key)) continue;
                if (!map.TryGetValue(key, out var list))
                {
                    list = [];
                    map[key] = list;
                }

                if (!list.Contains(id))
                    list.Add(id);
            }
        }

        return map;
    }

    public static IEnumerable<string> DeriveRouteKeys(string routeName)
    {
        if (string.IsNullOrWhiteSpace(routeName))
            yield break;

        var parts = SplitRouteName(routeName);
        if (parts.Length != 2)
            yield break;

        var a = RouteLocationNormalizer.Normalize(parts[0]);
        var b = RouteLocationNormalizer.Normalize(parts[1]);
        var key = RouteLocationNormalizer.BuildRouteKey(a, b);
        if (!string.IsNullOrWhiteSpace(key))
            yield return key;
    }

    private static string[] SplitRouteName(string name)
    {
        var separators = new[] { "↔", "⟷", " - ", " – ", " — ", "-", "–", "—" };
        foreach (var sep in separators)
        {
            var idx = name.IndexOf(sep, StringComparison.Ordinal);
            if (idx <= 0) continue;
            var left = name[..idx].Trim();
            var right = name[(idx + sep.Length)..].Trim();
            if (left.Length > 0 && right.Length > 0)
                return [left, right];
        }

        return [];
    }

    public static (VehicleType? Type, string? Note) ResolveVehicleType(RouteDemandReadinessSource src)
    {
        if (RouteDemandReadinessCalculator.TryParseVehicleType(src.AssignedVehicleType, out var assigned))
            return (assigned, "ASSIGNED_CAPTAIN");

        var prefs = src.PreferredVehicleTypes
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => RouteDemandReadinessCalculator.TryParseVehicleType(p, out var t) ? t : (VehicleType?)null)
            .Where(t => t is not null)
            .Select(t => t!.Value)
            .ToList();

        if (prefs.Count > 0)
        {
            var groups = prefs.GroupBy(x => x).OrderByDescending(g => g.Count()).ToList();
            var top = groups[0];
            if (groups.Count == 1 || top.Count() > prefs.Count / 2)
                return (top.Key, "PREFERRED_VEHICLE");
        }

        if (RouteDemandReadinessCalculator.TryParseVehicleType(src.RecommendedVehicleCode, out var recommended))
            return (recommended, "DEMAND_BAND_RECOMMENDATION");

        return (null, null);
    }

    private static bool IsMissingRelation(Exception ex, string relationName)
    {
        for (var e = ex; e != null; e = e.InnerException!)
        {
            var msg = e.Message ?? "";
            if (msg.Contains(relationName, StringComparison.OrdinalIgnoreCase) &&
                (msg.Contains("does not exist", StringComparison.OrdinalIgnoreCase) ||
                 msg.Contains("42P01", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }
}
