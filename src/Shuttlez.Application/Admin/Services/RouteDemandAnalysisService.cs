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

    public RouteDemandAnalysisService(IAppDbContext db) => _db = db;

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
        var filtered = ApplyFilters(await BuildGroupsAsync(cancellationToken), query);
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
        var group = (await BuildGroupsAsync(cancellationToken))
            .FirstOrDefault(g => g.RouteKey == routeKey);

        return group is null ? null : ToDetails(group);
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
        var filtered = ApplyFilters(await BuildGroupsAsync(cancellationToken), query);

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

        var group = (await BuildGroupsAsync(cancellationToken))
            .FirstOrDefault(g => g.RouteKey == routeKey);

        return group is null ? null : ToRow(group, 0);
    }

    private async Task<List<RouteDemandGroup>> BuildGroupsAsync(CancellationToken cancellationToken)
    {
        var passengers = await LoadPassengersAsync(cancellationToken);
        var states = await _db.RouteDemandGroupStates
            .Where(s => !s.IsDeleted)
            .Include(s => s.AssignedDriver!)
                .ThenInclude(d => d.User)
            .Include(s => s.AssignedDriver!)
                .ThenInclude(d => d.Vehicle)
            .ToListAsync(cancellationToken);

        var stateByKey = states.ToDictionary(s => s.RouteKey, StringComparer.Ordinal);

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
                if (state?.AssignedDriver is { } driver)
                {
                    captain = new RouteDemandCaptainDto(
                        driver.Id,
                        driver.User.FullName ?? driver.User.Phone,
                        driver.User.Phone,
                        driver.Vehicle?.Type.ToString() ?? driver.VehicleKind ?? "—",
                        driver.Vehicle?.Capacity ?? driver.Seats ?? 0,
                        driver.VerificationStatus.ToString());
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
                    FirstRequestAt = samples.Min(s => s.CreatedAt),
                    LastRequestAt = samples.Max(s => s.CreatedAt),
                };
            })
            .OrderByDescending(g => g.TotalRequests)
            .ThenByDescending(g => g.UniquePassengers)
            .ThenBy(g => g.FirstRequestAt)
            .ToList();
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
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.User.Phone,
                r.User.FullName,
                r.FromAddress,
                r.ToAddress,
                r.Status,
                r.Notes,
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
            group.AssignedDriverName);

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
                .ToList());
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
        public DateTime FirstRequestAt { get; init; }
        public DateTime LastRequestAt { get; init; }
    }

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
