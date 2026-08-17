using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Domain.Entities;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Admin.Handlers;

public record AdminTripsQuery(
    Guid? RouteId = null,
    Guid? DriverId = null,
    string? Status = null,
    DateTime? From = null,
    DateTime? To = null,
    int? Page = null,
    int? PageSize = null) : IRequest<PagedResult<AdminTripDto>>;

public record SaveTripCommand(Guid? Id, SaveTripRequest Request) : IRequest<AdminTripDto>;

public record DeleteTripCommand(Guid Id) : IRequest<bool>;

/// <summary>
/// جدولة رحلات متكرّرة على خط واحد: لكل يوم داخل المدى وكل موعد في القائمة.
/// <c>DaysOfWeek</c> أرقام 0..6 حيث 0 = الأحد؛ فارغة تعني كل الأيام.
/// </summary>
public record GenerateTripsRequest(
    Guid RouteId,
    Guid? DriverId,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<string> Times,
    IReadOnlyList<int>? DaysOfWeek,
    decimal PricePerSeat,
    int AvailableSeats);

public record GenerateTripsCommand(GenerateTripsRequest Request) : IRequest<int>;

public record AdminBookingsQuery(
    string? Search = null,
    Guid? TripId = null,
    Guid? UserId = null,
    string? Status = null,
    int? Page = null,
    int? PageSize = null) : IRequest<PagedResult<AdminBookingDto>>;

public record UpdateBookingStatusCommand(Guid Id, UpdateBookingStatusRequest Request)
    : IRequest<AdminBookingDto>;

public record AdminReviewsQuery(
    Guid? DriverId = null,
    int? MinStars = null,
    int? Page = null,
    int? PageSize = null) : IRequest<PagedResult<AdminReviewDto>>;

public record DeleteReviewCommand(Guid Id) : IRequest<bool>;

public class AdminTripHandlers :
    IRequestHandler<AdminTripsQuery, PagedResult<AdminTripDto>>,
    IRequestHandler<SaveTripCommand, AdminTripDto>,
    IRequestHandler<DeleteTripCommand, bool>,
    IRequestHandler<GenerateTripsCommand, int>,
    IRequestHandler<AdminBookingsQuery, PagedResult<AdminBookingDto>>,
    IRequestHandler<UpdateBookingStatusCommand, AdminBookingDto>,
    IRequestHandler<AdminReviewsQuery, PagedResult<AdminReviewDto>>,
    IRequestHandler<DeleteReviewCommand, bool>
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly IDriverRealtimeNotifier _realtime;
    private readonly IMemoryCache _cache;

    public AdminTripHandlers(
        IAppDbContext db,
        IDateTimeProvider clock,
        IDriverRealtimeNotifier realtime,
        IMemoryCache cache)
    {
        _db = db;
        _clock = clock;
        _realtime = realtime;
        _cache = cache;
    }

    public async Task<PagedResult<AdminTripDto>> Handle(
        AdminTripsQuery request,
        CancellationToken cancellationToken)
    {
        var page = PageRequest.From(request.Page, request.PageSize);
        var query = _db.Trips.Where(t => !t.IsDeleted);

        if (request.RouteId is not null)
        {
            query = query.Where(t => t.RouteId == request.RouteId);
        }

        if (request.DriverId is not null)
        {
            query = query.Where(t => t.DriverId == request.DriverId);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = AdminMapper.ParseTripStatus(request.Status);
            query = query.Where(t => t.Status == status);
        }

        if (request.From is not null)
        {
            var from = ToUtc(request.From.Value);
            query = query.Where(t => t.ScheduledAt >= from);
        }

        if (request.To is not null)
        {
            var to = ToUtc(request.To.Value);
            query = query.Where(t => t.ScheduledAt <= to);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.ScheduledAt)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(TripProjection())
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminTripDto>(items, page.Page, page.PageSize, total);
    }

    public async Task<AdminTripDto> Handle(
        SaveTripCommand request,
        CancellationToken cancellationToken)
    {
        var body = request.Request;
        await EnsureRouteAsync(body.RouteId, cancellationToken);
        await EnsureDriverAsync(body.DriverId, cancellationToken);

        if (body.AvailableSeats < 0)
        {
            throw new AppException("عدد المقاعد لا يمكن أن يكون سالباً");
        }
        if (body.PricePerSeat < 0)
        {
            throw new AppException("السعر لا يمكن أن يكون سالباً");
        }

        Trip trip;
        if (request.Id is null)
        {
            trip = new Trip();
            _db.Add(trip);
        }
        else
        {
            trip = await _db.Trips
                .FirstOrDefaultAsync(t => t.Id == request.Id && !t.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("الرحلة غير موجودة");
            trip.UpdatedAt = _clock.UtcNow;
            _db.Update(trip);
        }

        trip.RouteId = body.RouteId;
        trip.DriverId = body.DriverId == Guid.Empty ? null : body.DriverId;
        trip.ScheduledAt = ToUtc(body.ScheduledAt);
        trip.PricePerSeat = body.PricePerSeat;
        trip.AvailableSeats = body.AvailableSeats;
        trip.ReferenceCode = string.IsNullOrWhiteSpace(body.ReferenceCode)
            ? trip.ReferenceCode ?? BuildReferenceCode()
            : body.ReferenceCode.Trim();

        trip.Status = string.IsNullOrWhiteSpace(body.Status)
            ? (trip.DriverId is null ? TripStatus.Scheduled : TripStatus.DriverAssigned)
            : AdminMapper.ParseTripStatus(body.Status);

        if (trip.Status == TripStatus.InProgress && trip.StartedAt is null)
        {
            trip.StartedAt = _clock.UtcNow;
        }
        if (trip.Status == TripStatus.Completed && trip.CompletedAt is null)
        {
            trip.CompletedAt = _clock.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);

        await NotifyDriverTripsChangedAsync(trip.DriverId, cancellationToken);

        return await _db.Trips
            .Where(t => t.Id == trip.Id)
            .Select(TripProjection())
            .FirstAsync(cancellationToken);
    }

    public async Task<bool> Handle(
        DeleteTripCommand request,
        CancellationToken cancellationToken)
    {
        var trip = await _db.Trips
            .FirstOrDefaultAsync(t => t.Id == request.Id && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("الرحلة غير موجودة");

        var hasBookings = await _db.Bookings.AnyAsync(
            b => b.TripId == trip.Id && !b.IsDeleted && b.Status != BookingStatus.Cancelled,
            cancellationToken);

        if (hasBookings)
        {
            throw new AppException("الرحلة تحتوي حجوزات نشطة. ألغِها أولاً");
        }

        var driverId = trip.DriverId;
        trip.IsDeleted = true;
        trip.Status = TripStatus.Cancelled;
        trip.UpdatedAt = _clock.UtcNow;
        _db.Update(trip);
        await _db.SaveChangesAsync(cancellationToken);
        await NotifyDriverTripsChangedAsync(driverId, cancellationToken);
        return true;
    }

    public async Task<int> Handle(
        GenerateTripsCommand request,
        CancellationToken cancellationToken)
    {
        var body = request.Request;
        await EnsureRouteAsync(body.RouteId, cancellationToken);
        await EnsureDriverAsync(body.DriverId, cancellationToken);

        if (body.Times.Count == 0)
        {
            throw new AppException("أضف موعداً واحداً على الأقل");
        }
        if (body.EndDate.Date < body.StartDate.Date)
        {
            throw new AppException("تاريخ النهاية يجب أن يكون بعد تاريخ البداية");
        }

        var span = (body.EndDate.Date - body.StartDate.Date).Days;
        if (span > 180)
        {
            throw new AppException("المدى الأقصى للجدولة 180 يوماً");
        }

        var times = body.Times.Select(ParseTimeOfDay).ToList();
        var allowedDays = body.DaysOfWeek is null || body.DaysOfWeek.Count == 0
            ? null
            : body.DaysOfWeek.Select(d => (DayOfWeek)Math.Clamp(d, 0, 6)).ToHashSet();

        var existing = await _db.Trips
            .Where(t => t.RouteId == body.RouteId && !t.IsDeleted)
            .Select(t => t.ScheduledAt)
            .ToListAsync(cancellationToken);
        var existingSlots = existing.ToHashSet();

        var created = 0;
        for (var offset = 0; offset <= span; offset++)
        {
            var day = body.StartDate.Date.AddDays(offset);
            if (allowedDays is not null && !allowedDays.Contains(day.DayOfWeek))
            {
                continue;
            }

            foreach (var time in times)
            {
                var scheduledAt = ToUtc(day.Add(time));
                if (!existingSlots.Add(scheduledAt))
                {
                    continue;
                }

                _db.Add(new Trip
                {
                    RouteId = body.RouteId,
                    DriverId = body.DriverId == Guid.Empty ? null : body.DriverId,
                    ScheduledAt = scheduledAt,
                    PricePerSeat = body.PricePerSeat,
                    AvailableSeats = body.AvailableSeats,
                    Status = body.DriverId is null ? TripStatus.Scheduled : TripStatus.DriverAssigned,
                    ReferenceCode = BuildReferenceCode()
                });
                created++;
            }
        }

        if (created == 0)
        {
            throw new AppException("لا توجد مواعيد جديدة لإضافتها (كلها موجودة بالفعل)");
        }

        await _db.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task<PagedResult<AdminBookingDto>> Handle(
        AdminBookingsQuery request,
        CancellationToken cancellationToken)
    {
        var page = PageRequest.From(request.Page, request.PageSize);
        var query = _db.Bookings.Where(b => !b.IsDeleted);

        if (request.TripId is not null)
        {
            query = query.Where(b => b.TripId == request.TripId);
        }

        if (request.UserId is not null)
        {
            query = query.Where(b => b.UserId == request.UserId);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = AdminMapper.ParseBookingStatus(request.Status);
            query = query.Where(b => b.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(b =>
                b.User.Phone.Contains(term)
                || (b.ReferenceCode != null && b.ReferenceCode.Contains(term))
                || b.Trip.Route.Name.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(BookingProjection())
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminBookingDto>(items, page.Page, page.PageSize, total);
    }

    public async Task<AdminBookingDto> Handle(
        UpdateBookingStatusCommand request,
        CancellationToken cancellationToken)
    {
        var booking = await _db.Bookings
            .Include(b => b.Trip)
            .Include(b => b.Invoice)
            .FirstOrDefaultAsync(b => b.Id == request.Id && !b.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("الحجز غير موجود");

        var newStatus = AdminMapper.ParseBookingStatus(request.Request.Status);
        var wasActive = booking.Status is BookingStatus.Pending or BookingStatus.Confirmed;
        var willBeActive = newStatus is BookingStatus.Pending or BookingStatus.Confirmed;

        if (wasActive && !willBeActive)
        {
            booking.Trip.AvailableSeats += booking.SeatCount;
        }
        else if (!wasActive && willBeActive)
        {
            if (booking.Trip.AvailableSeats < booking.SeatCount)
            {
                throw new AppException("لا توجد مقاعد كافية لإعادة تنشيط الحجز");
            }
            booking.Trip.AvailableSeats -= booking.SeatCount;
        }

        booking.Status = newStatus;
        booking.UpdatedAt = _clock.UtcNow;

        if (booking.Invoice is not null)
        {
            booking.Invoice.Status = newStatus switch
            {
                BookingStatus.Confirmed => PaymentStatus.Paid,
                BookingStatus.Cancelled => PaymentStatus.Refunded,
                _ => PaymentStatus.Pending
            };
            booking.Invoice.PaidAt = newStatus == BookingStatus.Confirmed
                ? booking.Invoice.PaidAt ?? _clock.UtcNow
                : null;
            booking.Invoice.UpdatedAt = _clock.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return await _db.Bookings
            .Where(b => b.Id == booking.Id)
            .Select(BookingProjection())
            .FirstAsync(cancellationToken);
    }

    public async Task<PagedResult<AdminReviewDto>> Handle(
        AdminReviewsQuery request,
        CancellationToken cancellationToken)
    {
        var page = PageRequest.From(request.Page, request.PageSize);
        var query = _db.Reviews.Where(r => !r.IsDeleted);

        if (request.DriverId is not null)
        {
            query = query.Where(r => r.DriverId == request.DriverId);
        }

        if (request.MinStars is not null)
        {
            query = query.Where(r => r.Stars >= request.MinStars);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(r => new AdminReviewDto(
                r.Id,
                r.TripId,
                _db.Trips
                    .Where(t => t.Id == r.TripId)
                    .Select(t => t.Route.Name)
                    .FirstOrDefault() ?? "-",
                r.UserId,
                r.User.Phone,
                r.User.FullName,
                r.DriverId,
                _db.Drivers
                    .Where(d => d.Id == r.DriverId)
                    .Select(d => d.User.FullName ?? d.User.Phone)
                    .FirstOrDefault(),
                r.Stars,
                r.Comment,
                r.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminReviewDto>(items, page.Page, page.PageSize, total);
    }

    public async Task<bool> Handle(
        DeleteReviewCommand request,
        CancellationToken cancellationToken)
    {
        var review = await _db.Reviews
            .FirstOrDefaultAsync(r => r.Id == request.Id && !r.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("التقييم غير موجود");

        review.IsDeleted = true;
        review.UpdatedAt = _clock.UtcNow;
        _db.Update(review);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsureRouteAsync(Guid routeId, CancellationToken ct)
    {
        var exists = await _db.Routes.AnyAsync(r => r.Id == routeId && !r.IsDeleted, ct);
        if (!exists)
        {
            throw new NotFoundException("الخط غير موجود");
        }
    }

    private async Task EnsureDriverAsync(Guid? driverId, CancellationToken ct)
    {
        if (driverId is null || driverId == Guid.Empty) return;

        var exists = await _db.Drivers.AnyAsync(d => d.Id == driverId && !d.IsDeleted, ct);
        if (!exists)
        {
            throw new NotFoundException("الكابتن غير موجود");
        }
    }

    private static TimeSpan ParseTimeOfDay(string value)
    {
        if (TimeSpan.TryParse(value, out var parsed) && parsed >= TimeSpan.Zero && parsed < TimeSpan.FromDays(1))
        {
            return parsed;
        }
        throw new AppException($"موعد غير صالح: {value}");
    }

    private async Task NotifyDriverTripsChangedAsync(Guid? driverId, CancellationToken ct)
    {
        if (driverId is null || driverId == Guid.Empty)
        {
            return;
        }

        var userId = await _db.Drivers
            .AsNoTracking()
            .Where(d => d.Id == driverId && !d.IsDeleted)
            .Select(d => (Guid?)d.UserId)
            .FirstOrDefaultAsync(ct);

        if (userId is null)
        {
            return;
        }

        _cache.Remove($"driver-trips:{userId}");
        await _realtime.NotifyTripsChangedAsync(userId.Value, ct);
    }

    private static string BuildReferenceCode() =>
        "TRP-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private System.Linq.Expressions.Expression<Func<Trip, AdminTripDto>> TripProjection() =>
        t => new AdminTripDto(
            t.Id,
            t.RouteId,
            t.Route.Name,
            t.DriverId,
            t.Driver == null ? null : (t.Driver.User.FullName ?? t.Driver.User.Phone),
            t.Status == TripStatus.Scheduled ? "scheduled"
                : t.Status == TripStatus.DriverAssigned ? "driverassigned"
                : t.Status == TripStatus.InProgress ? "inprogress"
                : t.Status == TripStatus.Completed ? "completed" : "cancelled",
            t.ScheduledAt,
            t.StartedAt,
            t.CompletedAt,
            t.PricePerSeat,
            t.AvailableSeats,
            t.ReferenceCode,
            _db.Bookings.Count(b => b.TripId == t.Id && !b.IsDeleted),
            _db.Bookings
                .Where(b => b.TripId == t.Id && !b.IsDeleted && b.Status == BookingStatus.Confirmed)
                .Sum(b => (decimal?)b.TotalAmount) ?? 0m,
            t.CreatedAt);

    private static System.Linq.Expressions.Expression<Func<Booking, AdminBookingDto>> BookingProjection() =>
        b => new AdminBookingDto(
            b.Id,
            b.TripId,
            b.Trip.Route.Name,
            b.Trip.ScheduledAt,
            b.UserId,
            b.User.Phone,
            b.User.FullName,
            b.Status == BookingStatus.Pending ? "pending"
                : b.Status == BookingStatus.Confirmed ? "confirmed"
                : b.Status == BookingStatus.Cancelled ? "cancelled" : "expired",
            b.SeatCount,
            b.TotalAmount,
            b.PaymentMethod,
            b.ReferenceCode,
            b.Invoice == null
                ? null
                : b.Invoice.Status == PaymentStatus.Paid ? "paid"
                    : b.Invoice.Status == PaymentStatus.Failed ? "failed"
                    : b.Invoice.Status == PaymentStatus.Refunded ? "refunded" : "pending",
            b.CreatedAt);
}
