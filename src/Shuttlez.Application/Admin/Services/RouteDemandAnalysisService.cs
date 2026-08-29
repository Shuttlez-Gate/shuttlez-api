using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Domain.Entities;

namespace Shuttlez.Application.Admin.Services;

public interface IRouteDemandAnalysisService
{
    Task<RouteDemandSummaryDto> GetSummaryAsync(CancellationToken cancellationToken);

    Task<(IReadOnlyList<RouteDemandRowDto> Items, int Total)> GetRoutesAsync(
        RouteDemandAnalysisQuery query,
        CancellationToken cancellationToken);

    Task<RouteDemandDetailsDto?> GetDetailsAsync(string routeKey, CancellationToken cancellationToken);

    Task<IReadOnlyList<RouteDemandPassengerDto>> GetPassengersAsync(
        string routeKey,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RouteDemandExportRowDto>> ExportAsync(
        RouteDemandAnalysisQuery query,
        CancellationToken cancellationToken);

    Task<RouteDemandRowDto?> UpdateStatusAsync(
        string routeKey,
        UpdateRouteDemandStatusRequest request,
        CancellationToken cancellationToken);

    Task<RouteDemandRowDto?> MapRouteAsync(
        string routeKey,
        Guid routeId,
        Guid? adminUserId,
        CancellationToken cancellationToken);

    Task<RouteDemandRowDto?> UnmapRouteAsync(
        string routeKey,
        CancellationToken cancellationToken);

    Task<RouteLaunchPlanResponseDto> GetLaunchPlanAsync(
        RouteDemandAnalysisQuery query,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<VehicleCapacityInfoDto>> GetVehicleCapacitiesAsync(
        CancellationToken cancellationToken);
}

public sealed class RouteDemandAnalysisService : IRouteDemandAnalysisService
{
    private static readonly string[] AllowedStatuses =
    [
        "new_demand",
        "collecting_demand",
        "ready_for_captain",
        "captain_assigned",
        "ready_to_launch",
        "running",
        "paused",
        "rejected",
    ];

    private readonly IAppDbContext _db;
    private readonly IRouteDemandReadinessEnricher _readiness;
    private readonly IDateTimeProvider _clock;

    public RouteDemandAnalysisService(
        IAppDbContext db,
        IRouteDemandReadinessEnricher readiness,
        IDateTimeProvider clock)
    {
        _db = db;
        _readiness = readiness;
        _clock = clock;
    }

    public async Task<RouteDemandSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var groups = await BuildGroupsAsync(cancellationToken);
        var top = groups.FirstOrDefault();

        var allPassengers = groups.SelectMany(g => g.Passengers).ToList();

        return new RouteDemandSummaryDto(
            allPassengers.Count,
            allPassengers.Select(p => p.Phone).Distinct(StringComparer.Ordinal).Count(),
            groups.Count,
            top?.RouteLabel,
            top?.TotalRequests ?? 0);
    }

    public async Task<(IReadOnlyList<RouteDemandRowDto> Items, int Total)> GetRoutesAsync(
        RouteDemandAnalysisQuery query,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page ?? 1);
        var pageSize = Math.Clamp(query.PageSize ?? 20, 1, 100);
        var groups = await BuildGroupsAsync(cancellationToken);
        await AttachReadinessAsync(groups, cancellationToken);
        var filtered = ApplyFilters(groups, query);
        var total = filtered.Count;
        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select((g, index) => ToRow(g, (page - 1) * pageSize + index + 1))
            .ToList();

        return (items, total);
    }

    public async Task<RouteDemandDetailsDto?> GetDetailsAsync(
        string routeKey,
        CancellationToken cancellationToken)
    {
        var groups = await BuildGroupsAsync(cancellationToken);
        var group = groups.FirstOrDefault(g => g.RouteKey == routeKey);
        if (group is null) return null;
        await AttachReadinessAsync([group], cancellationToken);
        return ToDetails(group);
    }

    public async Task<IReadOnlyList<RouteDemandPassengerDto>> GetPassengersAsync(
        string routeKey,
        CancellationToken cancellationToken)
    {
        var group = (await BuildGroupsAsync(cancellationToken))
            .FirstOrDefault(g => g.RouteKey == routeKey);

        return group?.Passengers
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => p.ToDto())
            .ToList() ?? [];
    }

    public async Task<IReadOnlyList<RouteDemandExportRowDto>> ExportAsync(
        RouteDemandAnalysisQuery query,
        CancellationToken cancellationToken)
    {
        var groups = await BuildGroupsAsync(cancellationToken);
        await AttachReadinessAsync(groups, cancellationToken);
        var filtered = ApplyFilters(groups, query);

        return filtered
            .Select((g, index) =>
            {
                var preferredTime = g.Passengers
                    .Select(p => p.PreferredDepartureTime)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .GroupBy(t => t!)
                    .OrderByDescending(x => x.Count())
                    .Select(x => x.Key)
                    .FirstOrDefault();

                return new RouteDemandExportRowDto(
                    index + 1,
                    g.RouteLabel,
                    g.EndpointA,
                    g.EndpointB,
                    g.TotalRequests,
                    g.UniquePassengers,
                    g.ConfirmedPassengers,
                    g.Vehicle.Label,
                    g.Vehicle.Capacity,
                    Math.Max(0, g.Vehicle.Capacity - g.TotalRequests),
                    CalculatePriority(g.TotalRequests),
                    g.Status,
                    preferredTime,
                    g.AssignedDriverName,
                    g.FirstRequestAt);
            })
            .ToList();
    }

    public async Task<RouteDemandRowDto?> UpdateStatusAsync(
        string routeKey,
        UpdateRouteDemandStatusRequest request,
        CancellationToken cancellationToken)
    {
        var status = request.Status.Trim().ToLowerInvariant();
        if (!AllowedStatuses.Contains(status))
        {
            throw new InvalidOperationException(
                "حالة غير مسموحة. المسموح: " + string.Join(" / ", AllowedStatuses));
        }

        var state = await _db.RouteDemandGroupStates
            .FirstOrDefaultAsync(s => s.RouteKey == routeKey && !s.IsDeleted, cancellationToken);

        if (state is null)
        {
            state = new RouteDemandGroupState
            {
                RouteKey = routeKey,
                Status = status,
                AssignedDriverId = request.AssignedDriverId,
            };
            _db.Add(state);
        }
        else
        {
            state.Status = status;
            state.AssignedDriverId = request.AssignedDriverId;
            state.UpdatedAt = DateTime.UtcNow;
            _db.Update(state);
        }

        await _db.SaveChangesAsync(cancellationToken);

        var groups = await BuildGroupsAsync(cancellationToken);
        var group = groups.FirstOrDefault(g => g.RouteKey == routeKey);
        if (group is null) return null;
        await AttachReadinessAsync([group], cancellationToken);
        return ToRow(group, 0);
    }

    public async Task<RouteDemandRowDto?> MapRouteAsync(
        string routeKey,
        Guid routeId,
        Guid? adminUserId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(routeKey))
            throw new InvalidOperationException("مفتاح مجموعة الطلب مطلوب.");

        var groups = await BuildGroupsAsync(cancellationToken);
        if (groups.All(g => g.RouteKey != routeKey))
            throw new InvalidOperationException("مجموعة الطلب غير موجودة.");

        var route = await _db.Routes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == routeId && !r.IsDeleted && r.IsActive, cancellationToken);
        if (route is null)
            throw new InvalidOperationException("المسار الرسمي غير موجود أو غير نشط.");

        // One catalog Route may only map to one demand corridor at a time.
        try
        {
            var conflict = await _db.RouteDemandGroupStates
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    s => !s.IsDeleted &&
                         s.MappedRouteId == routeId &&
                         s.RouteKey != routeKey,
                    cancellationToken);
            if (conflict is not null)
            {
                throw new InvalidOperationException(
                    "هذا المسار مرتبط بالفعل بمجموعة طلب أخرى. أزل الربط السابق أولاً.");
            }
        }
        catch (Exception ex) when (IsMissingMappedRouteColumn(ex))
        {
            throw new InvalidOperationException(
                "ترحيل ربط المسار غير مُطبَّق على قاعدة البيانات. موافقة المشغّل مطلوبة.");
        }

        RouteDemandGroupState? state;
        try
        {
            state = await _db.RouteDemandGroupStates
                .FirstOrDefaultAsync(s => s.RouteKey == routeKey && !s.IsDeleted, cancellationToken);
        }
        catch (Exception ex) when (IsMissingMappedRouteColumn(ex))
        {
            throw new InvalidOperationException(
                "ترحيل ربط المسار غير مُطبَّق على قاعدة البيانات. موافقة المشغّل مطلوبة.");
        }

        var now = _clock.UtcNow;
        if (state is null)
        {
            state = new RouteDemandGroupState
            {
                RouteKey = routeKey,
                Status = "collecting_demand",
                MappedRouteId = routeId,
                MappedAt = now,
                MappedByUserId = adminUserId,
            };
            _db.Add(state);
        }
        else
        {
            state.MappedRouteId = routeId;
            state.MappedAt = now;
            state.MappedByUserId = adminUserId;
            state.UpdatedAt = now;
            _db.Update(state);
        }

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (IsMissingMappedRouteColumn(ex))
        {
            throw new InvalidOperationException(
                "ترحيل ربط المسار غير مُطبَّق على قاعدة البيانات. موافقة المشغّل مطلوبة.");
        }

        groups = await BuildGroupsAsync(cancellationToken);
        var group = groups.FirstOrDefault(g => g.RouteKey == routeKey);
        if (group is null) return null;
        await AttachReadinessAsync([group], cancellationToken);
        return ToRow(group, 0);
    }

    public async Task<RouteDemandRowDto?> UnmapRouteAsync(
        string routeKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(routeKey))
            throw new InvalidOperationException("مفتاح مجموعة الطلب مطلوب.");

        var state = await _db.RouteDemandGroupStates
            .FirstOrDefaultAsync(s => s.RouteKey == routeKey && !s.IsDeleted, cancellationToken);
        if (state is null)
            throw new InvalidOperationException("لا يوجد ربط محفوظ لهذه المجموعة.");

        state.MappedRouteId = null;
        state.MappedAt = null;
        state.MappedByUserId = null;
        state.UpdatedAt = _clock.UtcNow;
        _db.Update(state);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (IsMissingMappedRouteColumn(ex))
        {
            throw new InvalidOperationException(
                "ترحيل ربط المسار غير مُطبَّق على قاعدة البيانات. موافقة المشغّل مطلوبة.");
        }

        var groups = await BuildGroupsAsync(cancellationToken);
        var group = groups.FirstOrDefault(g => g.RouteKey == routeKey);
        if (group is null) return null;
        await AttachReadinessAsync([group], cancellationToken);
        return ToRow(group, 0);
    }

    public async Task<RouteLaunchPlanResponseDto> GetLaunchPlanAsync(
        RouteDemandAnalysisQuery query,
        CancellationToken cancellationToken)
    {
        // Planning status filter applied after mapping (READY_TO_LAUNCH ≠ READY).
        var preFilter = query with { LaunchStatus = null, ReadyToLaunch = null };
        var groups = await BuildGroupsAsync(cancellationToken);
        await AttachReadinessAsync(groups, cancellationToken);
        var filtered = ApplyFilters(groups, preFilter);

        var caps = await GetVehicleCapacitiesAsync(cancellationToken);
        var sourceByType = caps.ToDictionary(
            c => c.VehicleType,
            c => c.Source,
            StringComparer.OrdinalIgnoreCase);

        IEnumerable<RouteLaunchPlanDto> plans = filtered
            .Where(g => g.Readiness is not null)
            .Select(g =>
            {
                var src = g.Readiness!.VehicleType is string vt &&
                          sourceByType.TryGetValue(vt, out var s)
                    ? s
                    : null;
                return RouteLaunchPlanMapper.FromReadiness(
                    g.Readiness!,
                    g.RouteKey,
                    g.RouteLabel,
                    g.EndpointA,
                    g.EndpointB,
                    src);
            });

        if (!string.IsNullOrWhiteSpace(query.RouteKey))
        {
            var key = query.RouteKey.Trim();
            plans = plans.Where(p => p.RouteKey == key);
        }

        if (!string.IsNullOrWhiteSpace(query.LaunchStatus))
        {
            var st = query.LaunchStatus.Trim().ToUpperInvariant();
            plans = plans.Where(p => p.LaunchStatus == st);
        }

        if (query.PricingAvailable is bool pricingOk)
            plans = plans.Where(p => p.PricingAvailable == pricingOk);

        if (query.ReadyToLaunch == true)
            plans = plans.Where(p => p.LaunchStatus is "READY_TO_LAUNCH" or "FULL");

        var list = plans
            .OrderBy(p => RouteLaunchPlanMapper.PlanningSortRank(p.LaunchStatus))
            .ThenByDescending(p => p.Demand)
            .ThenBy(p => p.RouteLabel, StringComparer.Ordinal)
            .ToList();

        return new RouteLaunchPlanResponseDto(list, RouteLaunchPlanMapper.BuildSummary(list));
    }

    public async Task<IReadOnlyList<VehicleCapacityInfoDto>> GetVehicleCapacitiesAsync(
        CancellationToken cancellationToken)
    {
        var fleet = await _db.Vehicles
            .AsNoTracking()
            .Where(v => !v.IsDeleted && v.IsActive && v.Capacity > 0)
            .GroupBy(v => v.Type)
            .Select(g => new { Type = g.Key, Cap = g.Max(v => v.Capacity) })
            .ToListAsync(cancellationToken);

        var byType = fleet.ToDictionary(x => x.Type, x => x.Cap);
        var defaults = new Dictionary<Domain.Enums.VehicleType, int>
        {
            [Domain.Enums.VehicleType.CarShuttle] = 4,
            [Domain.Enums.VehicleType.MiniBus] = 13,
            [Domain.Enums.VehicleType.Bus] = 24,
        };

        return Enum.GetValues<Domain.Enums.VehicleType>()
            .Select(t =>
            {
                if (byType.TryGetValue(t, out var cap) && cap > 0)
                {
                    return new VehicleCapacityInfoDto(
                        t.ToString(),
                        RouteLaunchPlanMapper.VehicleDisplayName(t),
                        cap,
                        "VEHICLE_MASTER");
                }

                return new VehicleCapacityInfoDto(
                    t.ToString(),
                    RouteLaunchPlanMapper.VehicleDisplayName(t),
                    defaults.GetValueOrDefault(t, 0),
                    "DEFAULT_HINT");
            })
            .Where(x => x.Capacity > 0)
            .ToList();
    }

    private async Task AttachReadinessAsync(
        List<RouteDemandGroup> groups,
        CancellationToken cancellationToken)
    {
        if (groups.Count == 0) return;

        var sources = groups.Select(g => new RouteDemandReadinessSource(
            g.RouteKey,
            g.EndpointA,
            g.EndpointB,
            g.TotalRequests,
            g.ConfirmedPassengers,
            g.UniquePassengers,
            g.Vehicle.Code,
            g.Captain?.VehicleType,
            g.Passengers
                .Select(p => p.PreferredVehicleType)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v!)
                .ToList(),
            g.MappedRouteId)).ToList();

        var map = await _readiness.EnrichAsync(sources, _clock.UtcNow, cancellationToken);

        var routeIds = map.Values.Where(r => r.RouteId is not null).Select(r => r.RouteId!.Value).Distinct().ToList();
        var names = routeIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Routes
                .AsNoTracking()
                .Where(r => routeIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);

        var caps = await GetVehicleCapacitiesAsync(cancellationToken);
        var sourceByType = caps.ToDictionary(
            c => c.VehicleType,
            c => c.Source,
            StringComparer.OrdinalIgnoreCase);

        foreach (var g in groups)
        {
            if (!map.TryGetValue(g.RouteKey, out var readiness)) continue;
            var routeName = readiness.RouteId is Guid rid && names.TryGetValue(rid, out var n) ? n : null;
            string? capacitySource = null;
            if (!string.IsNullOrWhiteSpace(readiness.VehicleType?.ToString()) &&
                sourceByType.TryGetValue(readiness.VehicleType!.ToString()!, out var src))
            {
                capacitySource = src;
            }
            else if (readiness.Capacity is > 0)
            {
                capacitySource = "DEFAULT_HINT";
            }

            g.Readiness = MapReadinessDto(
                readiness,
                routeName,
                g.LastRequestAt,
                g.Vehicle.Capacity,
                capacitySource,
                g.RouteKey);
        }
    }

    private static RouteDemandReadinessDto MapReadinessDto(
        RouteDemandReadinessResult r,
        string? routeName,
        DateTime lastUpdated,
        int demandBandCapacity,
        string? capacitySource,
        string demandRouteKey)
    {
        var conflict = r.Capacity is int auth &&
                       demandBandCapacity > 0 &&
                       auth != demandBandCapacity;
        var reason = r.ReadinessReason;
        if (conflict)
        {
            reason +=
                $" · تعارض سعة: تقدير الطلب يعرض {demandBandCapacity} بينما السعة التشغيلية المعتمدة {r.Capacity}" +
                (string.IsNullOrWhiteSpace(capacitySource) ? "" : $" ({capacitySource})");
        }

        string? mappingCompatibility = null;
        if (string.Equals(r.RouteLinkSource, "EXPLICIT", StringComparison.Ordinal) &&
            !string.IsNullOrWhiteSpace(routeName))
        {
            mappingCompatibility = RouteDemandReadinessEnricher.IsExactBidirectionalMatch(demandRouteKey, routeName)
                ? "EXACT_BIDIRECTIONAL"
                : "MANUAL_OVERRIDE";
            if (mappingCompatibility == "MANUAL_OVERRIDE")
            {
                reason += " · ربط يدوي صريح: لا يوجد تطابق آمن (مفتاح ثنائي الاتجاه) مع اسم المسار.";
            }
        }
        else if (string.Equals(r.RouteLinkSource, "EXACT_KEY", StringComparison.Ordinal))
        {
            mappingCompatibility = "EXACT_BIDIRECTIONAL";
        }

        return new RouteDemandReadinessDto(
            r.RouteId,
            routeName,
            r.VehicleType?.ToString(),
            r.VehicleType switch
            {
                Domain.Enums.VehicleType.CarShuttle => "سيارة شاتلز",
                Domain.Enums.VehicleType.MiniBus => "ميكروباص",
                Domain.Enums.VehicleType.Bus => "أتوبيس",
                _ => null
            },
            r.Capacity,
            r.DemandCount,
            r.UniquePassengers,
            r.ConfirmedPassengers,
            r.OccupancyPercent,
            r.MinimumLaunchRiders,
            r.TargetOccupancy,
            r.RidersRequired,
            r.RemainingToTarget,
            r.PricingAvailable,
            r.PricingLinked,
            r.PricingSource,
            r.OneWayPrice,
            r.RoundTripPrice,
            r.WeeklyPrice,
            r.MonthlyPrice,
            r.CommissionPercent,
            r.CommissionType,
            r.LaunchPeriodDays,
            r.LaunchStartAt,
            r.LaunchEndAt,
            r.LaunchActive,
            r.FinancialAtMinimumOneWayGross,
            r.FinancialAtMinimumRoundTripGross,
            r.FinancialAtMinimumPlatformCommission,
            r.FinancialAtMinimumCaptainEarnings,
            RouteDemandReadinessCalculator.ToApiStatus(r.LaunchStatus),
            r.ReasonCode,
            reason,
            r.PricingRuleId,
            lastUpdated,
            demandBandCapacity > 0 ? demandBandCapacity : null,
            capacitySource,
            conflict,
            r.RouteLinkSource,
            mappingCompatibility);
    }

    private async Task<List<RouteDemandGroup>> BuildGroupsAsync(CancellationToken cancellationToken)
    {
        var passengers = await LoadPassengersAsync(cancellationToken);

        // Project only columns needed — avoid materializing full User.
        // MappedRouteId may be absent until Phase 2.2 migration is applied.
        var stateByKey = await LoadStateRowsAsync(cancellationToken);

        return passengers
            .GroupBy(p => p.RouteKey, StringComparer.Ordinal)
            .Select(group =>
            {
                var samples = group.ToList();
                var endpointLabels = ResolveEndpointLabels(samples);
                stateByKey.TryGetValue(group.Key, out var state);

                var totalRequests = samples.Count;
                var uniquePassengers = samples.Select(s => s.Phone).Distinct(StringComparer.Ordinal).Count();
                var confirmed = samples.Count(s => s.IsConfirmed);
                var routeType = InferDominantRouteType(samples);
                var vehicle = RecommendVehicle(totalRequests);
                var defaultStatus = totalRequests >= 3 ? "collecting_demand" : "new_demand";
                var status = state?.Status ?? defaultStatus;

                RouteDemandCaptainDto? captain = null;
                if (state?.DriverId is Guid driverId && !string.IsNullOrWhiteSpace(state.DriverPhone))
                {
                    captain = new RouteDemandCaptainDto(
                        driverId,
                        state.DriverName ?? state.DriverPhone,
                        state.DriverPhone,
                        state.VehicleLabel ?? "—",
                        state.Capacity ?? 0,
                        state.Verification ?? "—");
                }

                return new RouteDemandGroup
                {
                    RouteKey = group.Key,
                    RouteLabel = RouteLocationNormalizer.BuildDisplayLabel(endpointLabels.A, endpointLabels.B),
                    EndpointA = endpointLabels.A,
                    EndpointB = endpointLabels.B,
                    Passengers = samples,
                    TotalRequests = totalRequests,
                    UniquePassengers = uniquePassengers,
                    ConfirmedPassengers = confirmed,
                    RouteType = routeType,
                    Status = status,
                    AssignedDriverId = state?.AssignedDriverId,
                    AssignedDriverName = captain?.Name,
                    Captain = captain,
                    Vehicle = vehicle,
                    MappedRouteId = state?.MappedRouteId,
                    FirstRequestAt = samples.Min(s => s.CreatedAt),
                    LastRequestAt = samples.Max(s => s.CreatedAt),
                };
            })
            .OrderByDescending(g => g.TotalRequests)
            .ThenByDescending(g => g.UniquePassengers)
            .ThenBy(g => g.FirstRequestAt)
            .ToList();
    }

    private async Task<Dictionary<string, DemandStateRow>> LoadStateRowsAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var rows = await _db.RouteDemandGroupStates
                .AsNoTracking()
                .Where(s => !s.IsDeleted)
                .Select(s => new DemandStateRow(
                    s.RouteKey,
                    s.Status,
                    s.AssignedDriverId,
                    s.AssignedDriver != null ? (Guid?)s.AssignedDriver.Id : null,
                    s.AssignedDriver != null
                        ? (s.AssignedDriver.User.FullName ?? s.AssignedDriver.User.Phone)
                        : null,
                    s.AssignedDriver != null ? s.AssignedDriver.User.Phone : null,
                    s.AssignedDriver != null
                        ? (s.AssignedDriver.Vehicle != null
                            ? s.AssignedDriver.Vehicle.Type.ToString()
                            : s.AssignedDriver.VehicleKind)
                        : null,
                    s.AssignedDriver != null
                        ? (s.AssignedDriver.Vehicle != null
                            ? (int?)s.AssignedDriver.Vehicle.Capacity
                            : s.AssignedDriver.Seats)
                        : null,
                    s.AssignedDriver != null
                        ? s.AssignedDriver.VerificationStatus.ToString()
                        : null,
                    s.MappedRouteId))
                .ToListAsync(cancellationToken);
            return rows.ToDictionary(s => s.RouteKey, StringComparer.Ordinal);
        }
        catch (Exception ex) when (IsMissingMappedRouteColumn(ex))
        {
            var rows = await _db.RouteDemandGroupStates
                .AsNoTracking()
                .Where(s => !s.IsDeleted)
                .Select(s => new
                {
                    s.RouteKey,
                    s.Status,
                    s.AssignedDriverId,
                    DriverId = s.AssignedDriver != null ? (Guid?)s.AssignedDriver.Id : null,
                    DriverName = s.AssignedDriver != null
                        ? (s.AssignedDriver.User.FullName ?? s.AssignedDriver.User.Phone)
                        : null,
                    DriverPhone = s.AssignedDriver != null ? s.AssignedDriver.User.Phone : null,
                    VehicleLabel = s.AssignedDriver != null
                        ? (s.AssignedDriver.Vehicle != null
                            ? s.AssignedDriver.Vehicle.Type.ToString()
                            : s.AssignedDriver.VehicleKind)
                        : null,
                    Capacity = s.AssignedDriver != null
                        ? (s.AssignedDriver.Vehicle != null
                            ? (int?)s.AssignedDriver.Vehicle.Capacity
                            : s.AssignedDriver.Seats)
                        : null,
                    Verification = s.AssignedDriver != null
                        ? s.AssignedDriver.VerificationStatus.ToString()
                        : null,
                })
                .ToListAsync(cancellationToken);

            return rows.ToDictionary(
                s => s.RouteKey,
                s => new DemandStateRow(
                    s.RouteKey,
                    s.Status,
                    s.AssignedDriverId,
                    s.DriverId,
                    s.DriverName,
                    s.DriverPhone,
                    s.VehicleLabel,
                    s.Capacity,
                    s.Verification,
                    null),
                StringComparer.Ordinal);
        }
    }

    private static bool IsMissingMappedRouteColumn(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException!)
        {
            var msg = e.Message ?? "";
            if (msg.Contains("MappedRouteId", StringComparison.OrdinalIgnoreCase) &&
                (msg.Contains("does not exist", StringComparison.OrdinalIgnoreCase) ||
                 msg.Contains("42703", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<List<PassengerRow>> LoadPassengersAsync(CancellationToken cancellationToken)
    {
        var landing = await _db.LandingRouteLeads
            .Where(l => !l.IsDeleted)
            .Select(l => new PassengerRow
            {
                Id = l.Id,
                Source = "landing",
                PassengerName = null,
                Phone = l.Phone,
                From = $"{l.FromRegion}، {l.FromCity}",
                To = $"{l.ToRegion}، {l.ToCity}",
                WorkOrUniversity = l.UsageReason,
                PreferredDepartureTime = l.FromTime,
                PreferredReturnTime = l.ToTime,
                Days = l.UsageDays,
                LeadStatus = "lead",
                IsConfirmed = false,
                CreatedAt = l.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        var appRows = await _db.RouteRequests
            .Where(r => !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                Phone = r.User.Phone,
                FullName = r.User.FullName,
                r.FromAddress,
                r.ToAddress,
                r.Status,
                r.Notes,
                r.PreferredVehicleType,
                r.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        var app = appRows.Select(r =>
        {
            var notes = Admin.AdminMapper.ParseRouteRequestNotes(r.Notes);
            return new PassengerRow
            {
                Id = r.Id,
                Source = "app",
                PassengerName = r.FullName,
                Phone = r.Phone,
                From = r.FromAddress,
                To = r.ToAddress,
                WorkOrUniversity = notes.UsageReason,
                PreferredDepartureTime = notes.FromTime,
                PreferredReturnTime = notes.ToTime,
                Days = notes.UsageDays,
                PreferredVehicleType = r.PreferredVehicleType,
                LeadStatus = r.Status,
                IsConfirmed = r.Status is "approved" or "converted",
                CreatedAt = r.CreatedAt,
            };
        }).ToList();

        return landing.Concat(app)
            .Select(p =>
            {
                p.RouteKey = BuildPassengerRouteKey(p.From, p.To);
                return p;
            })
            .Where(p => !string.IsNullOrWhiteSpace(p.RouteKey))
            .ToList();
    }

    private static string BuildPassengerRouteKey(string from, string to)
    {
        var fromNorm = RouteLocationNormalizer.Normalize(from);
        var toNorm = RouteLocationNormalizer.Normalize(to);
        return RouteLocationNormalizer.BuildRouteKey(fromNorm, toNorm);
    }

    private static (string A, string B) ResolveEndpointLabels(IReadOnlyList<PassengerRow> samples)
    {
        if (samples.Count == 0)
        {
            return ("—", "—");
        }

        string PickLabel(string normalizedTarget)
        {
            return samples
                .SelectMany(s => new[]
                {
                    RouteLocationNormalizer.Normalize(s.From) == normalizedTarget ? s.From : null,
                    RouteLocationNormalizer.Normalize(s.To) == normalizedTarget ? s.To : null,
                })
                .Where(label => !string.IsNullOrWhiteSpace(label))
                .GroupBy(label => label!)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault() ?? normalizedTarget;
        }

        static string MostCommonLabel(IEnumerable<string> labels) =>
            labels
                .Where(label => !string.IsNullOrWhiteSpace(label))
                .GroupBy(label => label)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault() ?? "—";

        var normalizedEndpoints = samples
            .SelectMany(s => new[]
            {
                RouteLocationNormalizer.Normalize(s.From),
                RouteLocationNormalizer.Normalize(s.To),
            })
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        return normalizedEndpoints.Length switch
        {
            >= 2 => (PickLabel(normalizedEndpoints[0]), PickLabel(normalizedEndpoints[1])),
            1 => (MostCommonLabel(samples.Select(s => s.From)), MostCommonLabel(samples.Select(s => s.To))),
            _ => (MostCommonLabel(samples.Select(s => s.From)), MostCommonLabel(samples.Select(s => s.To))),
        };
    }

    private static List<RouteDemandGroup> ApplyFilters(
        IReadOnlyList<RouteDemandGroup> groups,
        RouteDemandAnalysisQuery query)
    {
        IEnumerable<RouteDemandGroup> result = groups;

        if (!string.IsNullOrWhiteSpace(query.RouteCategory))
        {
            var category = query.RouteCategory.Trim().ToLowerInvariant();
            result = category switch
            {
                "daily" => result.Where(g => g.RouteType is "daily" or "university"),
                "weekend" => result.Where(g => g.RouteType == "weekend"),
                _ => result,
            };
        }

        if (!string.IsNullOrWhiteSpace(query.RouteType))
        {
            var routeType = query.RouteType.Trim().ToLowerInvariant();
            result = result.Where(g => g.RouteType == routeType);
        }

        if (!string.IsNullOrWhiteSpace(query.From))
        {
            var from = RouteLocationNormalizer.Normalize(query.From);
            result = result.Where(g =>
                RouteLocationNormalizer.Normalize(g.EndpointA).Contains(from)
                || RouteLocationNormalizer.Normalize(g.EndpointB).Contains(from));
        }

        if (!string.IsNullOrWhiteSpace(query.To))
        {
            var to = RouteLocationNormalizer.Normalize(query.To);
            result = result.Where(g =>
                RouteLocationNormalizer.Normalize(g.EndpointA).Contains(to)
                || RouteLocationNormalizer.Normalize(g.EndpointB).Contains(to));
        }

        if (!string.IsNullOrWhiteSpace(query.VehicleType))
        {
            var vehicle = query.VehicleType.Trim().ToLowerInvariant();
            result = result.Where(g => MapVehicleFilter(g.Vehicle.Code) == vehicle);
        }

        if (!string.IsNullOrWhiteSpace(query.Priority))
        {
            var priority = query.Priority.Trim().ToLowerInvariant();
            result = result.Where(g => CalculatePriority(g.TotalRequests) == priority);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim().ToLowerInvariant();
            result = result.Where(g => g.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = RouteLocationNormalizer.Normalize(query.Search);
            result = result.Where(g =>
                RouteLocationNormalizer.Normalize(g.RouteLabel).Contains(term)
                || RouteLocationNormalizer.Normalize(g.EndpointA).Contains(term)
                || RouteLocationNormalizer.Normalize(g.EndpointB).Contains(term));
        }

        if (query.CreatedFrom.HasValue)
        {
            result = result.Where(g => g.LastRequestAt >= query.CreatedFrom.Value);
        }

        if (query.CreatedTo.HasValue)
        {
            result = result.Where(g => g.FirstRequestAt <= query.CreatedTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.LaunchStatus))
        {
            var launch = query.LaunchStatus.Trim().ToUpperInvariant();
            result = result.Where(g =>
                g.Readiness is not null &&
                string.Equals(g.Readiness.LaunchStatus, launch, StringComparison.OrdinalIgnoreCase));
        }

        if (query.PricingAvailable is bool pricingOk)
        {
            result = result.Where(g =>
                g.Readiness is not null && g.Readiness.PricingAvailable == pricingOk);
        }

        if (query.ReadyToLaunch == true)
        {
            result = result.Where(g =>
                g.Readiness is not null &&
                g.Readiness.LaunchStatus == "READY");
        }

        return result.ToList();
    }

    private static string MapVehicleFilter(string code) => code switch
    {
        "shuttlez_car" => "shuttlez",
        "microbus" => "microbus",
        "mini_bus" => "minibus",
        _ => code,
    };

    private static RouteDemandRowDto ToRow(RouteDemandGroup group, int rank) =>
        new(
            rank,
            group.RouteKey,
            group.RouteLabel,
            group.EndpointA,
            group.EndpointB,
            group.TotalRequests,
            group.ConfirmedPassengers,
            group.UniquePassengers,
            group.Vehicle.Label,
            group.Vehicle.Capacity,
            Math.Max(0, group.Vehicle.Capacity - group.TotalRequests),
            group.Vehicle.CapacityExceeded,
            CalculatePriority(group.TotalRequests),
            group.Status,
            group.RouteType,
            group.FirstRequestAt,
            group.LastRequestAt,
            group.AssignedDriverId,
            group.AssignedDriverName,
            group.Readiness);

    private static RouteDemandDetailsDto ToDetails(RouteDemandGroup group)
    {
        var remaining = Math.Max(0, group.Vehicle.Capacity - group.TotalRequests);
        var capacityPercent = group.Vehicle.Capacity <= 0
            ? 0
            : Math.Round(group.TotalRequests * 100d / group.Vehicle.Capacity, 1);
        var launch = BuildLaunchRecommendation(group);

        return new RouteDemandDetailsDto(
            group.RouteKey,
            group.RouteLabel,
            group.EndpointA,
            group.EndpointB,
            group.TotalRequests,
            group.ConfirmedPassengers,
            group.UniquePassengers,
            group.Vehicle.Label,
            group.Vehicle.Capacity,
            remaining,
            capacityPercent,
            group.Vehicle.CapacityExceeded,
            CalculatePriority(group.TotalRequests),
            group.Status,
            group.RouteType,
            launch.Title,
            launch.Reason,
            launch.NextAction,
            group.Captain,
            group.Passengers.OrderByDescending(p => p.CreatedAt).Select(p => p.ToDto()).ToList(),
            group.Passengers
                .Where(p => !string.IsNullOrWhiteSpace(p.PreferredDepartureTime))
                .GroupBy(p => p.PreferredDepartureTime!)
                .Select(g => new RouteDemandTimeBucketDto(g.Key, g.Count()))
                .OrderByDescending(x => x.PassengerCount)
                .ToList(),
            group.Passengers
                .Where(p => !string.IsNullOrWhiteSpace(p.Days))
                .SelectMany(p => p.Days!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .GroupBy(day => day)
                .Select(g => new RouteDemandDayBucketDto(g.Key, g.Count()))
                .OrderByDescending(x => x.PassengerCount)
                .ToList(),
            group.Readiness);
    }

    private static (string Title, string Reason, string NextAction) BuildLaunchRecommendation(RouteDemandGroup group)
    {
        if (group.Vehicle.CapacityExceeded)
        {
            return (
                "Split route or use larger fleet",
                $"{group.TotalRequests} passengers exceed recommended vehicle capacity.",
                "Review fleet assignment or split into multiple trips.");
        }

        if (group.TotalRequests >= 6)
        {
            return (
                "Recommended to prepare for launch",
                $"{group.TotalRequests} passengers requested this route.",
                "Find suitable captain and confirm passengers.");
        }

        if (group.TotalRequests >= 3)
        {
            return (
                "Collecting more demand",
                $"{group.TotalRequests} requests so far — keep promoting this corridor.",
                "Continue collecting passengers before assigning a captain.");
        }

        return (
            "Early demand signal",
            $"{group.TotalRequests} request(s) recorded for this corridor.",
            "Monitor demand growth before operational planning.");
    }

    private static string CalculatePriority(int demand) => demand switch
    {
        >= 6 => "high",
        >= 3 => "medium",
        _ => "low",
    };

    private static VehicleRecommendation RecommendVehicle(int demand)
    {
        if (demand <= 3)
        {
            return new VehicleRecommendation("shuttlez_car", "Shuttlez Car", 3, false);
        }

        if (demand <= 12)
        {
            return new VehicleRecommendation("microbus", "Microbus", 13, false);
        }

        if (demand <= 32)
        {
            return new VehicleRecommendation("mini_bus", "Mini Bus", 33, false);
        }

        return new VehicleRecommendation("capacity_exceeded", "Capacity exceeded", 33, true);
    }

    private static string InferDominantRouteType(IReadOnlyList<PassengerRow> samples) =>
        samples
            .Select(s => ClassifyRouteType(s.WorkOrUniversity))
            .GroupBy(x => x)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault() ?? "other";

    private static string ClassifyRouteType(string? usageReason)
    {
        var value = RouteLocationNormalizer.Normalize(usageReason);
        if (string.IsNullOrWhiteSpace(value))
        {
            return "other";
        }

        if (value.Contains("محاف") || value.Contains("governorate") || value.Contains("weekend")
            || value.Contains("اجاز") || value.Contains("نهاية"))
        {
            return "weekend";
        }

        if (value.Contains("جام") || value.Contains("كلية") || value.Contains("university") || value.Contains("college"))
        {
            return "university";
        }

        if (value.Contains("عمل") || value.Contains("work") || value.Contains("دوام") || value.Contains("office"))
        {
            return "daily";
        }

        return "other";
    }

    internal sealed record VehicleRecommendation(
        string Code,
        string Label,
        int Capacity,
        bool CapacityExceeded);

    private sealed class RouteDemandGroup
    {
        public required string RouteKey { get; init; }
        public required string RouteLabel { get; init; }
        public required string EndpointA { get; init; }
        public required string EndpointB { get; init; }
        public required List<PassengerRow> Passengers { get; init; }
        public int TotalRequests { get; init; }
        public int UniquePassengers { get; init; }
        public int ConfirmedPassengers { get; init; }
        public required string RouteType { get; init; }
        public required string Status { get; init; }
        public Guid? AssignedDriverId { get; init; }
        public string? AssignedDriverName { get; init; }
        public RouteDemandCaptainDto? Captain { get; init; }
        public required VehicleRecommendation Vehicle { get; init; }
        public Guid? MappedRouteId { get; init; }
        public DateTime FirstRequestAt { get; init; }
        public DateTime LastRequestAt { get; init; }
        public RouteDemandReadinessDto? Readiness { get; set; }
    }

    private sealed record DemandStateRow(
        string RouteKey,
        string Status,
        Guid? AssignedDriverId,
        Guid? DriverId,
        string? DriverName,
        string? DriverPhone,
        string? VehicleLabel,
        int? Capacity,
        string? Verification,
        Guid? MappedRouteId);

    private sealed class PassengerRow
    {
        public Guid Id { get; init; }
        public string Source { get; init; } = string.Empty;
        public string? PassengerName { get; init; }
        public string Phone { get; init; } = string.Empty;
        public string From { get; init; } = string.Empty;
        public string To { get; init; } = string.Empty;
        public string? WorkOrUniversity { get; init; }
        public string? PreferredDepartureTime { get; init; }
        public string? PreferredReturnTime { get; init; }
        public string? Days { get; init; }
        public string? PreferredVehicleType { get; init; }
        public string LeadStatus { get; init; } = string.Empty;
        public bool IsConfirmed { get; init; }
        public DateTime CreatedAt { get; init; }
        public string RouteKey { get; set; } = string.Empty;

        public RouteDemandPassengerDto ToDto() => new(
            Id,
            Source,
            PassengerName,
            Phone,
            From,
            To,
            WorkOrUniversity,
            PreferredDepartureTime,
            PreferredReturnTime,
            Days,
            LeadStatus,
            IsConfirmed,
            CreatedAt);
    }
}
