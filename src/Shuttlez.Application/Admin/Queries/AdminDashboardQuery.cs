using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Admin.Queries;

public record AdminDashboardQuery(int Days = 30) : IRequest<AdminDashboardDto>;

public class AdminDashboardQueryHandler : IRequestHandler<AdminDashboardQuery, AdminDashboardDto>
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public AdminDashboardQueryHandler(IAppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<AdminDashboardDto> Handle(
        AdminDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var days = Math.Clamp(request.Days, 7, 180);
        var now = _clock.UtcNow;
        var rangeStart = Utc(now.Date.AddDays(-(days - 1)));
        var previousStart = Utc(now.Date.AddDays(-(days * 2 - 1)));

        var metrics = await BuildMetricsAsync(rangeStart, previousStart, cancellationToken);
        var series = await BuildSeriesAsync(rangeStart, days, cancellationToken);
        var tripBreakdown = await BuildTripBreakdownAsync(cancellationToken);
        var bookingBreakdown = await BuildBookingBreakdownAsync(cancellationToken);
        var topRoutes = await BuildTopRoutesAsync(cancellationToken);
        var activity = await BuildActivityAsync(cancellationToken);

        return new AdminDashboardDto(
            metrics,
            series,
            tripBreakdown,
            bookingBreakdown,
            topRoutes,
            activity);
    }

    private async Task<List<MetricDto>> BuildMetricsAsync(
        DateTime rangeStart,
        DateTime previousStart,
        CancellationToken ct)
    {
        var users = _db.Users.Where(u => !u.IsDeleted);
        var bookings = _db.Bookings.Where(b => !b.IsDeleted);
        var trips = _db.Trips.Where(t => !t.IsDeleted);

        var totalUsers = await users.CountAsync(ct);
        var newUsers = await users.CountAsync(u => u.CreatedAt >= rangeStart, ct);
        var previousNewUsers = await users
            .CountAsync(u => u.CreatedAt >= previousStart && u.CreatedAt < rangeStart, ct);

        var totalDrivers = await _db.Drivers.CountAsync(d => !d.IsDeleted, ct);
        var onlineDrivers = await _db.Drivers.CountAsync(d => !d.IsDeleted && d.IsOnline, ct);

        var activeRoutes = await _db.Routes.CountAsync(r => !r.IsDeleted && r.IsActive, ct);
        var totalRoutes = await _db.Routes.CountAsync(r => !r.IsDeleted, ct);

        var upcomingTrips = await trips.CountAsync(
            t => t.Status == TripStatus.Scheduled || t.Status == TripStatus.DriverAssigned, ct);
        var completedTrips = await trips.CountAsync(t => t.Status == TripStatus.Completed, ct);

        var totalBookings = await bookings.CountAsync(ct);
        var newBookings = await bookings.CountAsync(b => b.CreatedAt >= rangeStart, ct);
        var previousBookings = await bookings
            .CountAsync(b => b.CreatedAt >= previousStart && b.CreatedAt < rangeStart, ct);

        var paidBookings = bookings.Where(b => b.Status == BookingStatus.Confirmed);
        var revenue = await paidBookings
            .Where(b => b.CreatedAt >= rangeStart)
            .SumAsync(b => (decimal?)b.TotalAmount, ct) ?? 0m;
        var previousRevenue = await paidBookings
            .Where(b => b.CreatedAt >= previousStart && b.CreatedAt < rangeStart)
            .SumAsync(b => (decimal?)b.TotalAmount, ct) ?? 0m;

        var openTickets = await _db.SupportTickets
            .CountAsync(t => !t.IsDeleted && t.Status != "closed", ct);
        var pendingRequests = await _db.RouteRequests
            .CountAsync(r => !r.IsDeleted && r.Status == "pending", ct);

        var leads = await _db.LandingRouteLeads.CountAsync(l => !l.IsDeleted, ct)
            + await _db.LandingWaitlistEntries.CountAsync(l => !l.IsDeleted, ct)
            + await _db.LandingCaptainLeads.CountAsync(l => !l.IsDeleted, ct);

        var avgRating = await _db.Reviews
            .Where(r => !r.IsDeleted)
            .AverageAsync(r => (double?)r.Stars, ct) ?? 0d;

        var walletBalance = await _db.Wallets
            .Where(w => !w.IsDeleted)
            .SumAsync(w => (decimal?)w.Balance, ct) ?? 0m;

        return
        [
            new MetricDto("revenue", "الإيرادات", revenue, previousRevenue, "currency"),
            new MetricDto("bookings", "الحجوزات", newBookings, previousBookings, "number"),
            new MetricDto("newUsers", "مستخدمون جدد", newUsers, previousNewUsers, "number"),
            new MetricDto("totalUsers", "إجمالي المستخدمين", totalUsers, null, "number"),
            new MetricDto("totalBookings", "إجمالي الحجوزات", totalBookings, null, "number"),
            new MetricDto("upcomingTrips", "رحلات قادمة", upcomingTrips, null, "number"),
            new MetricDto("completedTrips", "رحلات مكتملة", completedTrips, null, "number"),
            new MetricDto("activeRoutes", "خطوط نشطة", activeRoutes, totalRoutes, "number"),
            new MetricDto("drivers", "الكباتن", totalDrivers, onlineDrivers, "number"),
            new MetricDto("onlineDrivers", "كباتن متصلون", onlineDrivers, null, "number"),
            new MetricDto("openTickets", "تذاكر مفتوحة", openTickets, null, "number"),
            new MetricDto("pendingRequests", "طلبات خطوط معلّقة", pendingRequests, null, "number"),
            new MetricDto("leads", "عملاء محتملون", leads, null, "number"),
            new MetricDto("avgRating", "متوسط التقييم", Math.Round((decimal)avgRating, 2), null, "rating"),
            new MetricDto("walletBalance", "أرصدة المحافظ", walletBalance, null, "currency")
        ];
    }

    private async Task<List<TimeSeriesDto>> BuildSeriesAsync(
        DateTime rangeStart,
        int days,
        CancellationToken ct)
    {
        var bookingRows = await _db.Bookings
            .Where(b => !b.IsDeleted && b.CreatedAt >= rangeStart)
            .Select(b => new { b.CreatedAt, b.TotalAmount, b.Status })
            .ToListAsync(ct);

        var userRows = await _db.Users
            .Where(u => !u.IsDeleted && u.CreatedAt >= rangeStart)
            .Select(u => u.CreatedAt)
            .ToListAsync(ct);

        var tripRows = await _db.Trips
            .Where(t => !t.IsDeleted && t.ScheduledAt >= rangeStart)
            .Select(t => t.ScheduledAt)
            .ToListAsync(ct);

        var buckets = Enumerable.Range(0, days)
            .Select(offset => rangeStart.Date.AddDays(offset))
            .ToList();

        List<TimeSeriesPointDto> Build(Func<DateTime, decimal> selector) =>
            buckets.Select(day => new TimeSeriesPointDto(day, selector(day))).ToList();

        return
        [
            new TimeSeriesDto("revenue", "الإيرادات", Build(day =>
                bookingRows
                    .Where(b => b.Status == BookingStatus.Confirmed
                        && b.CreatedAt.Date == day)
                    .Sum(b => b.TotalAmount))),
            new TimeSeriesDto("bookings", "الحجوزات", Build(day =>
                bookingRows.Count(b => b.CreatedAt.Date == day))),
            new TimeSeriesDto("newUsers", "مستخدمون جدد", Build(day =>
                userRows.Count(created => created.Date == day))),
            new TimeSeriesDto("trips", "الرحلات", Build(day =>
                tripRows.Count(scheduled => scheduled.Date == day)))
        ];
    }

    private async Task<List<BreakdownSliceDto>> BuildTripBreakdownAsync(CancellationToken ct)
    {
        var rows = await _db.Trips
            .Where(t => !t.IsDeleted)
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return rows
            .Select(r => new BreakdownSliceDto(r.Status.ToSlug(), r.Count))
            .OrderByDescending(r => r.Value)
            .ToList();
    }

    private async Task<List<BreakdownSliceDto>> BuildBookingBreakdownAsync(CancellationToken ct)
    {
        var rows = await _db.Bookings
            .Where(b => !b.IsDeleted)
            .GroupBy(b => b.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return rows
            .Select(r => new BreakdownSliceDto(r.Status.ToSlug(), r.Count))
            .OrderByDescending(r => r.Value)
            .ToList();
    }

    private async Task<List<TopRouteDto>> BuildTopRoutesAsync(CancellationToken ct)
    {
        var rows = await _db.Routes
            .Where(r => !r.IsDeleted)
            .Select(r => new
            {
                r.Id,
                r.Name,
                TripCount = _db.Trips.Count(t => t.RouteId == r.Id && !t.IsDeleted),
                BookingCount = _db.Bookings.Count(b => !b.IsDeleted && b.Trip.RouteId == r.Id),
                Revenue = _db.Bookings
                    .Where(b => !b.IsDeleted
                        && b.Trip.RouteId == r.Id
                        && b.Status == BookingStatus.Confirmed)
                    .Sum(b => (decimal?)b.TotalAmount) ?? 0m
            })
            .OrderByDescending(r => r.BookingCount)
            .ThenByDescending(r => r.TripCount)
            .Take(8)
            .ToListAsync(ct);

        return rows
            .Select(r => new TopRouteDto(r.Id, r.Name, r.TripCount, r.BookingCount, r.Revenue))
            .ToList();
    }

    private async Task<List<ActivityItemDto>> BuildActivityAsync(CancellationToken ct)
    {
        var bookings = await _db.Bookings
            .Where(b => !b.IsDeleted)
            .OrderByDescending(b => b.CreatedAt)
            .Take(8)
            .Select(b => new ActivityItemDto(
                "booking",
                "حجز جديد على " + b.Trip.Route.Name,
                b.User.Phone,
                b.CreatedAt))
            .ToListAsync(ct);

        var users = await _db.Users
            .Where(u => !u.IsDeleted)
            .OrderByDescending(u => u.CreatedAt)
            .Take(8)
            .Select(u => new ActivityItemDto(
                "user",
                "مستخدم جديد " + (u.FullName ?? u.Phone),
                u.Phone,
                u.CreatedAt))
            .ToListAsync(ct);

        var requests = await _db.RouteRequests
            .Where(r => !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt)
            .Take(8)
            .Select(r => new ActivityItemDto(
                "routeRequest",
                "طلب خط سير: " + r.FromAddress + " ← " + r.ToAddress,
                r.User.Phone,
                r.CreatedAt))
            .ToListAsync(ct);

        var tickets = await _db.SupportTickets
            .Where(t => !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .Take(8)
            .Select(t => new ActivityItemDto(
                "ticket",
                "تذكرة دعم: " + t.Subject,
                t.User.Phone,
                t.CreatedAt))
            .ToListAsync(ct);

        return bookings
            .Concat(users)
            .Concat(requests)
            .Concat(tickets)
            .OrderByDescending(a => a.At)
            .Take(15)
            .ToList();
    }

    private static DateTime Utc(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
