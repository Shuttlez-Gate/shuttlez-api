using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.Trips.DTOs;
using Shuttlez.Application.Trips.Queries;
using Shuttlez.Domain.Entities;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Trips.Handlers;

public class TripHandlers :
    IRequestHandler<GetUserTripsQuery, TripListResponse>,
    IRequestHandler<GetTripDetailsQuery, TripDetailsDto>,
    IRequestHandler<GetTripInvoiceQuery, TripInvoiceDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public TripHandlers(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<TripListResponse> Handle(
        GetUserTripsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId();

        var bookings = await _db.Bookings
            .Include(b => b.Trip)
                .ThenInclude(t => t.Route)
            .Include(b => b.Trip)
                .ThenInclude(t => t.Driver!)
                    .ThenInclude(d => d.User)
            .Include(b => b.Trip)
                .ThenInclude(t => t.Driver!)
                    .ThenInclude(d => d.Vehicle)
            .Where(b => b.UserId == userId && b.Status != BookingStatus.Cancelled)
            .OrderByDescending(b => b.Trip.ScheduledAt)
            .ToListAsync(cancellationToken);

        var now = _clock.UtcNow;

        var items = bookings
            .Select(b => MapListItem(b.Trip, b, now))
            .ToList();

        TripListItemDto? current = items.FirstOrDefault(i => i.Status == "current");
        var upcoming = items.Where(i => i.Status == "upcoming").ToList();
        var history = items.Where(i => i.Status is "completed" or "cancelled").ToList();

        return new TripListResponse(current, upcoming, history);
    }

    public async Task<TripDetailsDto> Handle(
        GetTripDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId();

        var booking = await _db.Bookings
            .Include(b => b.Trip)
                .ThenInclude(t => t.Route)
                    .ThenInclude(r => r.Stops)
            .Include(b => b.Trip)
                .ThenInclude(t => t.Driver!)
                    .ThenInclude(d => d.User)
            .Include(b => b.Trip)
                .ThenInclude(t => t.Driver!)
                    .ThenInclude(d => d.Vehicle)
            .FirstOrDefaultAsync(
                b => b.TripId == request.TripId && b.UserId == userId,
                cancellationToken)
            ?? throw new NotFoundException("الرحلة غير موجودة");

        return MapDetails(booking.Trip, booking, _clock.UtcNow);
    }

    public async Task<TripInvoiceDto> Handle(
        GetTripInvoiceQuery request,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId();

        var booking = await _db.Bookings
            .Include(b => b.Trip)
                .ThenInclude(t => t.Route)
            .Include(b => b.Invoice)
            .Include(b => b.User)
            .FirstOrDefaultAsync(
                b => b.TripId == request.TripId && b.UserId == userId,
                cancellationToken)
            ?? throw new NotFoundException("الرحلة غير موجودة");

        var trip = booking.Trip;
        var amount = booking.TotalAmount;
        var baseFee = Math.Round(amount * 0.875m, 2);
        var extras = amount - baseFee;

        return new TripInvoiceDto(
            trip.ReferenceCode ?? trip.Id.ToString(),
            $"{ArDayName(trip.ScheduledAt.DayOfWeek)} {trip.ScheduledAt:dd/MM/yyyy} - {trip.ScheduledAt:HH:mm} - نقداً",
            booking.User.FullName ?? "مسافر",
            [
                new InvoiceLineItemDto("المبلغ الاجمالي", $"{amount:0} ج.م", true),
                new InvoiceLineItemDto("رسوم الرحلة الأساسية", $"{baseFee:0.00} ج.م", false),
                new InvoiceLineItemDto("تكلفة المسافة", $"{extras / 4:0.00} ج.م", false),
                new InvoiceLineItemDto("تكلفة الزمن", $"{extras / 4:0.00} ج.م", false),
                new InvoiceLineItemDto("تكلفة الانتظار", $"{extras / 4:0.00} ج.م", false),
                new InvoiceLineItemDto("تكلفة البوابات", $"{extras / 4:0.00} ج.م", false),
                new InvoiceLineItemDto("الضريبة", $"{extras / 4:0.00} ج.م", false)
            ]);
    }

    private Guid RequireUserId() =>
        _currentUser.UserId ?? throw new UnauthorizedAppException("غير مصرح");

    private static TripListItemDto MapListItem(Trip trip, Booking booking, DateTime now)
    {
        var status = ResolveStatus(trip, now);
        var vehicle = trip.Driver?.Vehicle;
        var progress = trip.Status == TripStatus.InProgress ? 0.73 : (double?)null;

        return new TripListItemDto(
            trip.Id,
            trip.ReferenceCode ?? $"#TR{trip.ScheduledAt:yy}-{trip.ScheduledAt:yyyy}",
            $"{ArDayName(trip.ScheduledAt.DayOfWeek)} {trip.ScheduledAt:dd/MM/yyyy} - {trip.ScheduledAt:HH:mm}",
            trip.ScheduledAt.Date,
            VehicleLabel(vehicle?.Type ?? VehicleType.MiniBus),
            trip.Route.Name.Split(" - ").FirstOrDefault() ?? trip.Route.Name,
            trip.Route.Name.Contains(" - ")
                ? trip.Route.Name.Split(" - ").Last()
                : trip.Route.Description ?? trip.Route.Name,
            status,
            VehicleAssetKey(vehicle?.Type ?? VehicleType.MiniBus),
            trip.Route.StartLatitude,
            trip.Route.StartLongitude,
            trip.Route.EndLatitude,
            trip.Route.EndLongitude,
            trip.Status == TripStatus.InProgress ? "الوصول 07:03" : null,
            trip.Status == TripStatus.InProgress ? "الى طريق النصر" : null,
            progress);
    }

    private static TripDetailsDto MapDetails(Trip trip, Booking booking, DateTime now)
    {
        var listItem = MapListItem(trip, booking, now);
        var vehicle = trip.Driver?.Vehicle;
        var driver = trip.Driver?.User;
        var stops = trip.Route.Stops
            .OrderBy(s => s.Order)
            .Select((s, i) => new TripTimelineStopDto(
                i == 0 ? "start" : i == trip.Route.Stops.Count - 1 ? "end" : "middle",
                s.Name,
                i == 0 ? "10 دقائق - محطة بنزين توتال" : "أمام طيبة مول",
                i == 0 && trip.Status == TripStatus.InProgress ? "التالي" : null,
                trip.Status == TripStatus.InProgress ? trip.ScheduledAt.AddHours(i).ToString("HH:mm") : null))
            .ToList();

        if (stops.Count == 0)
        {
            stops =
            [
                new TripTimelineStopDto("start", "شارع الطيران", "10 دقائق - محطة بنزين توتال", null, null),
                new TripTimelineStopDto("middle", "شارع الطيران", "أمام طيبة مول", null, null),
                new TripTimelineStopDto("end", "حلوان", "10 دقائق - موقف توشكى", null, null)
            ];
        }

        return new TripDetailsDto(
            listItem,
            VehicleDescription(vehicle),
            driver?.FullName ?? "محمد رضا السيد",
            driver?.AvatarUrl ?? "https://i.pravatar.cc/96?img=12",
            (double)(trip.Driver?.RatingAverage ?? 4.8m),
            null,
            $"{booking.SeatCount} مقعد",
            $"{booking.TotalAmount:0} ج.م - نقداً",
            trip.ScheduledAt.ToString("HH:mm"),
            trip.ScheduledAt.AddHours(3).ToString("HH:mm"),
            new TripRoutePointDto(
                trip.Route.Description ?? trip.Route.Name,
                trip.ScheduledAt.ToString("HH:mm"),
                unchecked((int)0xFF7367F6)),
            new TripRoutePointDto(
                trip.Route.Name,
                trip.ScheduledAt.AddHours(3).ToString("HH:mm"),
                unchecked((int)0xFF29E77C)),
            stops,
            trip.Route.Description ?? "طريق السويس، مدينة الشروق - القاهرة",
            trip.Route.Name,
            trip.Status == TripStatus.InProgress ? "15 دق" : null,
            trip.Status == TripStatus.InProgress ? "24 كم" : null,
            VehicleLabel(vehicle?.Type ?? VehicleType.MiniBus),
            vehicle != null ? $"أبيض - {vehicle.PlateNumber}" : "أبيض - م د 123",
            trip.Status == TripStatus.InProgress ? 1 : 0);
    }

    private static string ResolveStatus(Trip trip, DateTime now)
    {
        if (trip.Status == TripStatus.InProgress)
            return "current";
        if (trip.Status == TripStatus.Completed)
            return "completed";
        if (trip.Status == TripStatus.Cancelled)
            return "cancelled";
        if (trip.ScheduledAt <= now)
            return "completed";

        return "upcoming";
    }

    private static string MapStatus(TripStatus status) => status switch
    {
        TripStatus.Scheduled or TripStatus.DriverAssigned => "upcoming",
        TripStatus.InProgress => "current",
        TripStatus.Completed => "completed",
        TripStatus.Cancelled => "cancelled",
        _ => "upcoming"
    };

    private static string VehicleLabel(VehicleType type) => type switch
    {
        VehicleType.CarShuttle => "عربية شاتيل",
        VehicleType.Bus => "اتوبيس شاتيل",
        _ => "ميني باص"
    };

    private static string VehicleAssetKey(VehicleType type) => type switch
    {
        VehicleType.CarShuttle => "car",
        VehicleType.Bus => "bus",
        _ => "miniBus"
    };

    private static string VehicleDescription(Vehicle? vehicle)
    {
        if (vehicle == null) return "ميني باص - أبيض - م د 123";
        return $"{VehicleLabel(vehicle.Type)} - أبيض - {vehicle.PlateNumber}";
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
}
