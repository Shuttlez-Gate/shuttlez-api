using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shuttlez.Application.Bookings.DTOs;
using Shuttlez.Application.Bookings.Queries;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.RouteMatching;
using Shuttlez.Application.RouteMatching.Models;
using Shuttlez.Domain.Entities;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Bookings.Handlers;

public class BookingHandlers :
    IRequestHandler<GetBookingPreviewQuery, BookingPreviewDto>,
    IRequestHandler<CreateBookingCommand, CreateBookingResponse>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IRouteMatchingService _routeMatching;
    private readonly RouteMatchingOptions _matchingOptions;

    private static readonly string[] BadgeVariants = ["green", "cyan", "coral"];

    public BookingHandlers(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IRouteMatchingService routeMatching,
        IOptions<RouteMatchingOptions> matchingOptions)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _routeMatching = routeMatching;
        _matchingOptions = matchingOptions.Value;
    }

    public async Task<BookingPreviewDto> Handle(
        GetBookingPreviewQuery request,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId();

        var sourceAddress = string.IsNullOrWhiteSpace(request.SourceAddress)
            ? "طريق السويس - مدينة الشروق"
            : request.SourceAddress.Trim();
        var destinationAddress = string.IsNullOrWhiteSpace(request.DestinationAddress)
            ? "الستاد، يوسف عباس - مدينة نصر"
            : request.DestinationAddress.Trim();
        var vehicleType = MapVehicleType(request.VehicleTypeIndex);

        var route = await FindClosestRoute(
            request.SourceLatitude,
            request.SourceLongitude,
            request.DestinationLatitude,
            request.DestinationLongitude,
            cancellationToken);

        var today = _clock.UtcNow.Date;
        var visibleDays = BuildVisibleWorkDays(today);

        var rangeStart = visibleDays.First();
        var rangeEnd = visibleDays.Last().AddDays(1);

        var trips = await _db.Trips
            .Include(t => t.Route)
            .Include(t => t.Driver!)
                .ThenInclude(d => d.Vehicle)
            .Where(t =>
                t.RouteId == route.Id &&
                (t.Status == TripStatus.Scheduled ||
                 (t.Status == TripStatus.DriverAssigned && t.AvailableSeats > 0)) &&
                t.ScheduledAt >= rangeStart &&
                t.ScheduledAt < rangeEnd &&
                (t.Driver == null || t.Driver.Vehicle == null || t.Driver.Vehicle.Type == vehicleType))
            .OrderBy(t => t.ScheduledAt)
            .ToListAsync(cancellationToken);

        var tripIds = trips.Select(t => t.Id).ToList();
        var userBookedTripIds = tripIds.Count == 0
            ? []
            : await _db.Bookings
                .Where(b =>
                    b.UserId == userId &&
                    b.Status != BookingStatus.Cancelled &&
                    b.Status != BookingStatus.Expired &&
                    tripIds.Contains(b.TripId))
                .Select(b => b.TripId)
                .ToListAsync(cancellationToken);

        var bookedSet = userBookedTripIds.ToHashSet();

        var dateChips = new List<PreviewDateChipDto>();
        var offersPerDay = new List<IReadOnlyList<ShuttleOfferDto>>();

        for (var i = 0; i < visibleDays.Count; i++)
        {
            var day = visibleDays[i];
            var isSelected = day == today;
            dateChips.Add(new PreviewDateChipDto(
                ArDayName(day.DayOfWeek),
                $"{day.Day:00}/{day.Month:00}",
                isSelected));

            var dayTrips = trips
                .Where(t => t.ScheduledAt.Date == day)
                .ToList();

            offersPerDay.Add(dayTrips
                .Select((trip, offerIndex) =>
                    MapTripToOffer(
                        trip,
                        offerIndex,
                        sourceAddress,
                        destinationAddress,
                        bookedSet.Contains(trip.Id),
                        _clock.UtcNow))
                .ToList());
        }

        // لو النهاردة مفيش عروض (مثلاً التوليد يبدأ من بكرة) اختار أول يوم فيه رحلات.
        var preferredIndex = -1;
        for (var i = 0; i < offersPerDay.Count; i++)
        {
            if (offersPerDay[i].Count > 0)
            {
                preferredIndex = i;
                break;
            }
        }

        if (preferredIndex < 0)
        {
            preferredIndex = dateChips.FindIndex(c => c.IsSelected);
            if (preferredIndex < 0 && dateChips.Count > 0)
            {
                preferredIndex = 0;
            }
        }

        if (preferredIndex >= 0 && dateChips.Count > 0)
        {
            dateChips = dateChips
                .Select((c, idx) => c with { IsSelected = idx == preferredIndex })
                .ToList();
        }

        return new BookingPreviewDto(
            sourceAddress,
            destinationAddress,
            dateChips,
            offersPerDay);
    }

    public async Task<CreateBookingResponse> Handle(
        CreateBookingCommand request,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var seatCount = request.Request.SeatCount <= 0 ? 1 : request.Request.SeatCount;

        var trip = await _db.Trips
            .FirstOrDefaultAsync(
                t => t.Id == request.Request.TripId &&
                     (t.Status == TripStatus.Scheduled || t.Status == TripStatus.DriverAssigned),
                cancellationToken)
            ?? throw new NotFoundException("الرحلة غير متاحة للحجز");

        if (trip.ScheduledAt <= _clock.UtcNow)
            throw new AppException("انتهى وقت الرحلة");

        if (trip.AvailableSeats < seatCount)
            throw new AppException("لا توجد مقاعد كافية");

        var existing = await _db.Bookings
            .AnyAsync(
                b => b.TripId == trip.Id &&
                     b.UserId == userId &&
                     b.Status != BookingStatus.Cancelled &&
                     b.Status != BookingStatus.Expired,
                cancellationToken);

        if (existing)
            throw new AppException("لديك حجز على هذه الرحلة بالفعل");

        var total = trip.PricePerSeat * seatCount;
        var reference = $"BK-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";

        var booking = new Booking
        {
            TripId = trip.Id,
            UserId = userId,
            Status = BookingStatus.Confirmed,
            SeatCount = seatCount,
            TotalAmount = total,
            PaymentMethod = string.IsNullOrWhiteSpace(request.Request.PaymentMethod)
                ? "cash"
                : request.Request.PaymentMethod.Trim(),
            ReferenceCode = reference
        };

        trip.AvailableSeats -= seatCount;
        if (trip.AvailableSeats <= 0)
            trip.Status = TripStatus.DriverAssigned;

        _db.Add(booking);
        _db.Add(new Invoice
        {
            BookingId = booking.Id,
            Amount = total,
            Status = PaymentStatus.Pending
        });

        var tripRef = trip.ReferenceCode ?? reference;
        _db.Add(new Notification
        {
            UserId = userId,
            Title = $"رحلة جديدة برقم {tripRef}",
            Body = "لقد قمت بحجز رحلة جديدة بنجاح.",
            Type = "booking",
            IsRead = false
        });

        await _db.SaveChangesAsync(cancellationToken);

        return new CreateBookingResponse(
            booking.Id,
            trip.Id,
            reference,
            total,
            booking.Status.ToString(),
            "تم حجز الرحلة بنجاح");
    }

    private Guid RequireUserId() =>
        _currentUser.UserId ?? throw new UnauthorizedAppException("غير مصرح");

    private async Task<Route> FindClosestRoute(
        double sourceLat,
        double sourceLng,
        double destLat,
        double destLng,
        CancellationToken cancellationToken)
    {
        var origin = new GeoCoordinate(sourceLat, sourceLng);
        var destination = new GeoCoordinate(destLat, destLng);
        var now = _clock.UtcNow;

        var bufferDegrees = _matchingOptions.MaxDistanceMeters / 111_320d;
        var corridorMinLat = Math.Min(origin.Latitude, destination.Latitude) - bufferDegrees;
        var corridorMaxLat = Math.Max(origin.Latitude, destination.Latitude) + bufferDegrees;
        var corridorMinLng = Math.Min(origin.Longitude, destination.Longitude) - bufferDegrees;
        var corridorMaxLng = Math.Max(origin.Longitude, destination.Longitude) + bufferDegrees;

        var activeRouteRows = await _db.Routes
            .AsNoTracking()
            .Where(r =>
                r.IsActive &&
                !r.IsDeleted &&
                r.EncodedPolyline != null &&
                (r.BoundsMinLatitude == null ||
                 (r.BoundsMaxLatitude >= corridorMinLat &&
                  r.BoundsMinLatitude <= corridorMaxLat &&
                  r.BoundsMaxLongitude >= corridorMinLng &&
                  r.BoundsMinLongitude <= corridorMaxLng)))
            .Select(r => new
            {
                r.Id,
                r.EncodedPolyline,
                r.BoundsMinLatitude,
                r.BoundsMaxLatitude,
                r.BoundsMinLongitude,
                r.BoundsMaxLongitude,
                NextDepartureTime = r.Trips
                    .Where(t =>
                        !t.IsDeleted &&
                        (t.Status == TripStatus.Scheduled || t.Status == TripStatus.DriverAssigned) &&
                        t.ScheduledAt >= now &&
                        t.AvailableSeats > 0)
                    .OrderBy(t => t.ScheduledAt)
                    .Select(t => (DateTime?)t.ScheduledAt)
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        var activeRoutes = activeRouteRows
            .Where(r => !string.IsNullOrWhiteSpace(r.EncodedPolyline))
            .Select(r => new ActiveRouteMatchInput(
                r.Id,
                r.EncodedPolyline!,
                r.NextDepartureTime,
                r.BoundsMinLatitude,
                r.BoundsMaxLatitude,
                r.BoundsMinLongitude,
                r.BoundsMaxLongitude))
            .ToList();

        var matches = _routeMatching.MatchUserWithExistingRoutes(origin, destination, activeRoutes);
        if (matches.Count > 0)
        {
            var matchedRouteId = matches[0].RouteId;
            return await _db.Routes
                .FirstAsync(r => r.Id == matchedRouteId, cancellationToken);
        }

        var routes = await _db.Routes
            .Where(r => r.IsActive && !r.IsDeleted)
            .ToListAsync(cancellationToken);

        if (routes.Count == 0)
            throw new NotFoundException("لا توجد مسارات متاحة");

        return routes
            .OrderBy(r =>
                HaversineKm(sourceLat, sourceLng, r.StartLatitude, r.StartLongitude) +
                HaversineKm(destLat, destLng, r.EndLatitude, r.EndLongitude))
            .First();
    }

    private static VehicleType MapVehicleType(int index) => index switch
    {
        0 => VehicleType.CarShuttle,
        2 => VehicleType.Bus,
        _ => VehicleType.MiniBus
    };

    private static List<DateTime> BuildVisibleWorkDays(DateTime today)
    {
        var saturday = today.AddDays(-((int)today.DayOfWeek - (int)DayOfWeek.Saturday + 7) % 7);
        var days = new List<DateTime>();

        for (var offset = 0; offset < 7 && days.Count < 6; offset++)
        {
            var candidate = saturday.AddDays(offset);
            if (candidate.DayOfWeek == DayOfWeek.Friday) continue;
            if (candidate < today) continue;
            days.Add(candidate);
        }

        while (days.Count < 6)
        {
            saturday = saturday.AddDays(7);
            for (var offset = 0; offset < 6; offset++)
            {
                var candidate = saturday.AddDays(offset);
                if (candidate.DayOfWeek == DayOfWeek.Friday) continue;
                days.Add(candidate);
                if (days.Count == 6) break;
            }
        }

        return days;
    }

    private static ShuttleOfferDto MapTripToOffer(
        Trip trip,
        int offerIndex,
        string sourceAddress,
        string destinationAddress,
        bool alreadyBookedByUser,
        DateTime now)
    {
        var plate = trip.Driver?.Vehicle?.PlateNumber ?? $"B-YT-{5904 + offerIndex}";
        var capacity = trip.Driver?.Vehicle?.Capacity;
        var seats = trip.AvailableSeats;
        if (capacity is > 0 && seats > capacity)
            seats = capacity.Value;
        var noSeats = seats <= 0;
        var isPast = trip.ScheduledAt <= now;
        var unavailable = noSeats || alreadyBookedByUser || isPast;

        var pickupTime = trip.ScheduledAt.ToString("HH:mm");
        var dropoffTime = trip.ScheduledAt.AddHours(3).ToString("HH:mm");
        var walkMinutes = 5 + offerIndex % 4;

        string seatsLabel;
        if (alreadyBookedByUser)
        {
            seatsLabel = "محجوزة من قبلك";
        }
        else if (isPast)
        {
            seatsLabel = "انتهى الوقت";
        }
        else if (noSeats)
        {
            seatsLabel = "متبقي 0 مقعد";
        }
        else if (seats == 1)
        {
            seatsLabel = "متبقي مقعد واحد";
        }
        else
        {
            seatsLabel = $"متبقي {seats} مقعد";
        }

        return new ShuttleOfferDto(
            trip.Id,
            $"{walkMinutes} دق، لنقطة التحرك",
            sourceAddress,
            pickupTime,
            destinationAddress,
            dropoffTime,
            $"{walkMinutes + 3} دق، لنقطة التحرك",
            plate,
            BadgeVariants[offerIndex % BadgeVariants.Length],
            $"{trip.PricePerSeat:0} ج.م",
            "باقة غير محدودة .",
            offerIndex % 3 == 1 ? unchecked((int)0xFF03344F) : unchecked((int)0xFF565656),
            seatsLabel,
            unavailable ? unchecked((int)0xFF969696) : unchecked((int)0xFF565656),
            unavailable,
            unavailable);
    }

    private static string ArDayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "الاثنين",
        DayOfWeek.Tuesday => "الثلاثاء",
        DayOfWeek.Wednesday => "الأربعاء",
        DayOfWeek.Thursday => "الخميس",
        DayOfWeek.Friday => "الجمعة",
        DayOfWeek.Saturday => "السبت",
        DayOfWeek.Sunday => "الأحد",
        _ => string.Empty
    };

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
}
