using MediatR;

using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Options;

using Shuttlez.Application.Common;

using Shuttlez.Application.Common.Interfaces;

using Shuttlez.Application.Landing;

using Shuttlez.Application.Landing.DTOs;

using Shuttlez.Application.Landing.Services;

using Shuttlez.Domain.Entities;



namespace Shuttlez.Application.Landing.Queries;



public record GetPopularRoutesQuery(string? Language = null) : IRequest<IReadOnlyList<PopularRouteDto>>;



public record GetLandingRoutesQuery(string? Language = null) : IRequest<IReadOnlyList<LandingRouteDto>>;



public record GetLandingRouteMapQuery(Guid RouteId, string? Language = null) : IRequest<LandingRouteMapDto>;



public record GetLandingPageConfigQuery : IRequest<LandingPageConfigDto>;



public class LandingQueryHandlers :

    IRequestHandler<GetPopularRoutesQuery, IReadOnlyList<PopularRouteDto>>,

    IRequestHandler<GetLandingRoutesQuery, IReadOnlyList<LandingRouteDto>>,

    IRequestHandler<GetLandingRouteMapQuery, LandingRouteMapDto>,

    IRequestHandler<GetLandingPageConfigQuery, LandingPageConfigDto>

{

    private const string MeetingPointAr = "أقرب نقطة التقاء - 10دق";

    private const string MeetingPointEn = "Nearest meeting point - 10 min";



    private static readonly string[] BadgeTones = ["rose", "cyan", "green"];



    private readonly IAppDbContext _db;

    private readonly CaptainLaunchOfferSettings _offer;



    public LandingQueryHandlers(
        IAppDbContext db,
        IOptions<CaptainLaunchOfferSettings> offer)
    {
        _db = db;
        _offer = offer.Value;
    }



    public async Task<IReadOnlyList<PopularRouteDto>> Handle(

        GetPopularRoutesQuery request,

        CancellationToken cancellationToken)

    {

        var groups = await _db.LandingRouteLeads

            .GroupBy(l => new { l.FromRegion, l.FromCity, l.ToRegion, l.ToCity })

            .Select(g => new

            {

                g.Key.FromRegion,

                g.Key.FromCity,

                g.Key.ToRegion,

                g.Key.ToCity,

                Count = g.Count()

            })

            .OrderByDescending(x => x.Count)

            .Take(6)

            .ToListAsync(cancellationToken);



        var fromLeads = groups

            .Select(g => new PopularRouteDto(

                FormatLocationLabel(g.FromRegion, g.FromCity, request.Language),

                FormatLocationLabel(g.ToRegion, g.ToCity, request.Language),

                g.Count))

            .ToList();



        if (fromLeads.Count >= 3)

            return fromLeads;



        var fromRoutes = await BuildPopularFromActiveRoutesAsync(request.Language, cancellationToken);

        var merged = new List<PopularRouteDto>();



        foreach (var item in fromLeads.Concat(fromRoutes))

        {

            if (merged.Any(m => m.From == item.From && m.To == item.To))

                continue;

            merged.Add(item);

            if (merged.Count >= 6)

                break;

        }



        return merged;

    }



    public async Task<IReadOnlyList<LandingRouteDto>> Handle(

        GetLandingRoutesQuery request,

        CancellationToken cancellationToken)

    {

        var routes = await _db.Routes

            .Where(r => r.IsActive && !r.IsDeleted)

            .OrderBy(r => r.Name)

            .ToListAsync(cancellationToken);



        if (routes.Count == 0)

            return [];



        var routeIds = routes.Select(r => r.Id).ToList();



        var stopCounts = await _db.Stops

            .Where(s => routeIds.Contains(s.RouteId) && !s.IsDeleted)

            .GroupBy(s => s.RouteId)

            .Select(g => new { RouteId = g.Key, Count = g.Count() })

            .ToDictionaryAsync(x => x.RouteId, x => x.Count, cancellationToken);



        var firstStops = await _db.Stops

            .Where(s => routeIds.Contains(s.RouteId) && !s.IsDeleted)

            .OrderBy(s => s.Order)

            .ToListAsync(cancellationToken);



        var firstStopByRoute = firstStops

            .GroupBy(s => s.RouteId)

            .ToDictionary(g => g.Key, g => g.First());



        var plates = await _db.Trips

            .Where(t => routeIds.Contains(t.RouteId) && t.DriverId != null)

            .OrderByDescending(t => t.ScheduledAt)

            .Select(t => new { t.RouteId, Plate = t.Driver!.Vehicle!.PlateNumber })

            .ToListAsync(cancellationToken);



        var plateByRoute = plates

            .GroupBy(p => p.RouteId)

            .ToDictionary(g => g.Key, g => g.First().Plate);



        var waitlistCounts = await _db.LandingWaitlistEntries

            .Where(w => w.RouteId != null && routeIds.Contains(w.RouteId.Value))

            .GroupBy(w => w.RouteId!.Value)

            .Select(g => new { RouteId = g.Key, Count = g.Count() })

            .ToDictionaryAsync(x => x.RouteId, x => x.Count, cancellationToken);



        var items = new List<LandingRouteDto>();

        for (var i = 0; i < routes.Count; i++)

        {

            var route = routes[i];

            var (from, to) = LandingLabelLocalizer.LocalizeRouteName(route.Name, request.Language);

            firstStopByRoute.TryGetValue(route.Id, out var firstStop);

            stopCounts.TryGetValue(route.Id, out var stopsCount);

            plateByRoute.TryGetValue(route.Id, out var plate);

            waitlistCounts.TryGetValue(route.Id, out var waitlistCount);



            var nearestStreet = firstStop?.Name ?? route.Description ?? from;

            items.Add(new LandingRouteDto(

                route.Id,

                from,

                to,

                stopsCount,

                plate ?? $"R-{i + 1:00}",

                BadgeTones[i % BadgeTones.Length],

                LandingLabelLocalizer.Localize(nearestStreet, request.Language),

                LocalizeMeetingPoint(request.Language),

                waitlistCount));

        }



        return items;

    }



    public async Task<LandingRouteMapDto> Handle(

        GetLandingRouteMapQuery request,

        CancellationToken cancellationToken)

    {

        var route = await _db.Routes

            .FirstOrDefaultAsync(

                r => r.Id == request.RouteId && r.IsActive && !r.IsDeleted,

                cancellationToken)

            ?? throw new NotFoundException("المسار غير موجود");



        var stops = await _db.Stops

            .Where(s => s.RouteId == route.Id && !s.IsDeleted)

            .OrderBy(s => s.Order)

            .ToListAsync(cancellationToken);



        var (from, to) = LandingLabelLocalizer.LocalizeRouteName(route.Name, request.Language);



        var mapStops = stops

            .Select(s => new LandingMapStopDto(

                LandingLabelLocalizer.Localize(s.Name, request.Language),

                s.Latitude,

                s.Longitude,

                s.Order))

            .ToList();



        return new LandingRouteMapDto(

            route.Id,

            from,

            to,

            new LandingMapPointDto(from, route.StartLatitude, route.StartLongitude, "08:00"),

            new LandingMapPointDto(to, route.EndLatitude, route.EndLongitude, "09:30"),

            mapStops);

    }



    public async Task<LandingPageConfigDto> Handle(

        GetLandingPageConfigQuery request,

        CancellationToken cancellationToken)

    {

        var waitlistCount = await _db.LandingWaitlistEntries

            .CountAsync(cancellationToken);

        var captainOffer = await BuildCaptainOfferAsync(cancellationToken);



        return new LandingPageConfigDto(

            [

                new LandingVehicleOptionDto("mini_bus", "ميني باص", "Mini bus"),

                new LandingVehicleOptionDto("van", "فان", "Van"),

                new LandingVehicleOptionDto("microbus", "ميكروباص", "Microbus"),

            ],

            "تم انضمامك لقائمة الانتظار بنجاح",

            "تم استلام طلب التسجيل ككابتن. سنتواصل معك قريباً",

            "تم تسجيل طلبك بنجاح. سنتواصل معك قريباً",

            waitlistCount,

            captainOffer);

    }



    private async Task<List<PopularRouteDto>> BuildPopularFromActiveRoutesAsync(

        string? language,

        CancellationToken cancellationToken)

    {

        var routes = await _db.Routes

            .Where(r => r.IsActive && !r.IsDeleted)

            .OrderBy(r => r.Name)

            .Take(6)

            .ToListAsync(cancellationToken);



        var routeIds = routes.Select(r => r.Id).ToList();

        var bookingCounts = await _db.Trips

            .Where(t => routeIds.Contains(t.RouteId))

            .GroupBy(t => t.RouteId)

            .Select(g => new { RouteId = g.Key, Count = g.Count() })

            .ToDictionaryAsync(x => x.RouteId, x => x.Count, cancellationToken);



        return routes.Select((route, index) =>

        {

            var (from, to) = LandingLabelLocalizer.LocalizeRouteName(route.Name, language);

            bookingCounts.TryGetValue(route.Id, out var count);

            var requestCount = count > 0 ? count * 7 + 42 : 60 + index * 11;

            return new PopularRouteDto(from, to, requestCount);

        }).ToList();

    }



    private static string FormatLocationLabel(

        string region,

        string city,

        string? language) =>

        EgyptRouteLocations.IsEnglish(language)

            ? $"{LandingLabelLocalizer.Localize(region, language)} - {LandingLabelLocalizer.Localize(city, language)}"

            : $"{region} - {city}";



    private static string LocalizeMeetingPoint(string? language) =>

        EgyptRouteLocations.IsEnglish(language) ? MeetingPointEn : MeetingPointAr;

    private async Task<CaptainLaunchOfferDto> BuildCaptainOfferAsync(
        CancellationToken cancellationToken)
    {
        var totalSlots = Math.Max(1, _offer.TotalSlots);
        var profitPercent = Math.Clamp(_offer.ProfitPercent, 0, 100);
        var firstTrips = Math.Max(1, _offer.FirstTrips);
        var durationMonths = Math.Max(1, _offer.DurationMonths);

        var driverPhones = await _db.Drivers
            .Where(d => !d.IsDeleted)
            .Select(d => d.User.Phone)
            .ToListAsync(cancellationToken);

        var leadPhones = await _db.LandingCaptainLeads
            .Where(l => !l.IsDeleted)
            .Select(l => l.Phone)
            .ToListAsync(cancellationToken);

        var registeredCount = driverPhones
            .Concat(leadPhones)
            .Select(NormalizePhone)
            .Where(phone => phone.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Count();

        var remainingSlots = Math.Max(0, totalSlots - registeredCount);

        return new CaptainLaunchOfferDto(
            totalSlots,
            registeredCount,
            remainingSlots,
            profitPercent,
            firstTrips,
            durationMonths);
    }

    private static string NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return string.Empty;
        }

        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("20") && digits.Length > 10)
        {
            digits = digits[2..];
        }

        if (digits.StartsWith('0') && digits.Length >= 11)
        {
            digits = digits[1..];
        }

        return digits;
    }

}

