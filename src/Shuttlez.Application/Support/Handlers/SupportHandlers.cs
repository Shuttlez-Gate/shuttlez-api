using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.Support.DTOs;
using Shuttlez.Application.Trips.DTOs;
using Shuttlez.Domain.Entities;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Support.Handlers;

public record GetSupportTicketsQuery(string Tab) : IRequest<IReadOnlyList<SupportTicketDto>>;

public record GetSupportMessagesQuery(Guid TicketId) : IRequest<IReadOnlyList<SupportMessageDto>>;

public record CreateSupportTicketCommand(CreateSupportTicketRequest Request)
    : IRequest<CreateSupportTicketResponse>;

public record SendSupportMessageCommand(Guid TicketId, SendSupportMessageRequest Request)
    : IRequest<SendSupportMessageResponse>;

public class SupportHandlers :
    IRequestHandler<GetSupportTicketsQuery, IReadOnlyList<SupportTicketDto>>,
    IRequestHandler<GetSupportMessagesQuery, IReadOnlyList<SupportMessageDto>>,
    IRequestHandler<CreateSupportTicketCommand, CreateSupportTicketResponse>,
    IRequestHandler<SendSupportMessageCommand, SendSupportMessageResponse>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ISupportChatRealtimeNotifier _realtime;

    public SupportHandlers(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        ISupportChatRealtimeNotifier realtime)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _realtime = realtime;
    }

    public async Task<IReadOnlyList<SupportTicketDto>> Handle(
        GetSupportTicketsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var isCompletedTab = request.Tab.Equals("completed", StringComparison.OrdinalIgnoreCase);

        var tickets = await _db.SupportTickets
            .Include(t => t.Trip!)
                .ThenInclude(t => t.Route)
            .Include(t => t.Trip!)
                .ThenInclude(t => t.Driver!)
                    .ThenInclude(d => d.Vehicle)
            .Where(t => t.UserId == userId && !t.IsDeleted)
            .Where(t => isCompletedTab
                ? t.Status == "closed"
                : t.Status != "closed")
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        var tripIds = tickets
            .Where(t => t.TripId.HasValue)
            .Select(t => t.TripId!.Value)
            .ToList();

        var bookings = tripIds.Count == 0
            ? []
            : await _db.Bookings
                .Where(b => b.UserId == userId && tripIds.Contains(b.TripId))
                .ToDictionaryAsync(b => b.TripId, cancellationToken);

        var ticketIds = tickets.Select(t => t.Id).ToList();
        var lastMessages = ticketIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.SupportMessages
                .Where(m => ticketIds.Contains(m.TicketId) && !m.IsDeleted)
                .GroupBy(m => m.TicketId)
                .Select(g => new
                {
                    TicketId = g.Key,
                    Content = g.OrderByDescending(m => m.CreatedAt)
                        .Select(m => m.Content)
                        .FirstOrDefault()
                })
                .ToDictionaryAsync(x => x.TicketId, x => x.Content ?? string.Empty, cancellationToken);

        return tickets
            .Select(ticket =>
            {
                bookings.TryGetValue(ticket.TripId ?? Guid.Empty, out var booking);
                lastMessages.TryGetValue(ticket.Id, out var lastMessage);
                return MapTicket(ticket, booking, isCompletedTab, lastMessage);
            })
            .ToList();
    }

    public async Task<IReadOnlyList<SupportMessageDto>> Handle(
        GetSupportMessagesQuery request,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId();

        var ticket = await _db.SupportTickets
            .FirstOrDefaultAsync(
                t => t.Id == request.TicketId && t.UserId == userId && !t.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException("البلاغ غير موجود");

        var messages = await _db.SupportMessages
            .Where(m => m.TicketId == ticket.Id && !m.IsDeleted)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        return messages
            .Select(m => new SupportMessageDto(
                m.Id,
                m.IsFromSupport,
                m.Content,
                m.CreatedAt.ToString("HH:mm")))
            .ToList();
    }

    public async Task<CreateSupportTicketResponse> Handle(
        CreateSupportTicketCommand request,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        Trip? trip = null;
        Booking? booking = null;

        if (request.Request.TripId.HasValue)
        {
            var tripId = request.Request.TripId.Value;

            // راكب: التذكرة مربوطة بحجزه على الرحلة.
            booking = await _db.Bookings
                .Include(b => b.Trip)
                    .ThenInclude(t => t.Route)
                .Include(b => b.Trip)
                    .ThenInclude(t => t.Driver!)
                        .ThenInclude(d => d.Vehicle)
                .FirstOrDefaultAsync(
                    b => b.UserId == userId && b.TripId == tripId && !b.IsDeleted,
                    cancellationToken);

            trip = booking?.Trip;

            // كابتن: التذكرة مربوطة برحلة يُشغّلها.
            if (trip is null)
            {
                trip = await _db.Trips
                    .Include(t => t.Route)
                    .Include(t => t.Driver!)
                        .ThenInclude(d => d.Vehicle)
                    .FirstOrDefaultAsync(
                        t => t.Id == tripId
                            && !t.IsDeleted
                            && t.Driver != null
                            && t.Driver.UserId == userId
                            && !t.Driver.IsDeleted,
                        cancellationToken)
                    ?? throw new NotFoundException("الرحلة غير موجودة أو غير مرتبطة بحسابك");
            }
        }

        var subject = string.IsNullOrWhiteSpace(request.Request.Subject)
            ? trip?.ReferenceCode ?? "بلاغ دعم فني"
            : request.Request.Subject.Trim();

        var ticket = new SupportTicket
        {
            UserId = userId,
            TripId = trip?.Id,
            Subject = subject,
            Status = "open",
        };

        _db.Add(ticket);

        var initial = string.IsNullOrWhiteSpace(request.Request.InitialMessage)
            ? "مرحباً، أحتاج مساعدة بخصوص رحلتي."
            : request.Request.InitialMessage.Trim();

        _db.Add(new SupportMessage
        {
            Ticket = ticket,
            SenderId = userId,
            IsFromSupport = false,
            Content = initial,
        });

        _db.Add(new SupportMessage
        {
            Ticket = ticket,
            SenderId = userId,
            IsFromSupport = true,
            Content = "مرحباً بك، فريق الدعم الفني جاهز لمساعدتك.",
        });

        await NotifyAdminsAsync(
            title: "تذكرة دعم جديدة",
            body: $"{subject} — من {await ResolveUserLabelAsync(userId, cancellationToken)}",
            cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return new CreateSupportTicketResponse(ticket.Id, "تم إنشاء البلاغ بنجاح");
    }

    public async Task<SendSupportMessageResponse> Handle(
        SendSupportMessageCommand request,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId();

        var ticket = await _db.SupportTickets
            .FirstOrDefaultAsync(
                t => t.Id == request.TicketId && t.UserId == userId && !t.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException("البلاغ غير موجود");

        if (string.IsNullOrWhiteSpace(request.Request.Content))
            throw new AppException("الرسالة فارغة");

        var message = new SupportMessage
        {
            TicketId = ticket.Id,
            SenderId = userId,
            IsFromSupport = false,
            Content = request.Request.Content.Trim(),
        };

        _db.Add(message);
        ticket.UpdatedAt = _clock.UtcNow;

        await NotifyAdminsAsync(
            title: "رسالة دعم جديدة",
            body: message.Content.Length > 120 ? message.Content[..120] + "…" : message.Content,
            cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        await _realtime.NotifyMessageAsync(
            ticket.Id,
            message.Id,
            isFromSupport: false,
            message.Content,
            message.CreatedAt == default ? _clock.UtcNow : message.CreatedAt,
            cancellationToken);

        return new SendSupportMessageResponse(message.Id, "تم إرسال الرسالة");
    }

    private SupportTicketDto MapTicket(
        SupportTicket ticket,
        Booking? booking,
        bool isCompletedTab,
        string? lastMessage)
    {
        TripListItemDto? tripDto = null;
        var seatsLabel = "1 مقعد";
        var paymentLabel = "—";

        if (ticket.Trip != null && booking != null)
        {
            tripDto = MapTripListItem(ticket.Trip, booking);
            seatsLabel = booking.SeatCount == 1
                ? "1 مقعد"
                : $"{booking.SeatCount} مقعد";
            paymentLabel = $"{booking.TotalAmount:0} ج.م - نقداً";
        }
        else if (ticket.Trip != null)
        {
            // تذكرة كابتن أو بلاغ بدون حجز راكب — نعرض بيانات الرحلة فقط.
            tripDto = MapTripListItemWithoutBooking(ticket.Trip);
            paymentLabel = "—";
        }

        return new SupportTicketDto(
            ticket.Id,
            isCompletedTab ? "completed" : "current",
            isCompletedTab ? "completed" : "active",
            ticket.Subject,
            string.IsNullOrWhiteSpace(lastMessage) ? null : lastMessage,
            tripDto,
            seatsLabel,
            paymentLabel);
    }

    private async Task NotifyAdminsAsync(string title, string body, CancellationToken ct)
    {
        var adminIds = await _db.Users
            .Where(u => !u.IsDeleted && u.IsActive && u.UserType == UserType.Admin)
            .Select(u => u.Id)
            .ToListAsync(ct);

        foreach (var adminId in adminIds)
        {
            _db.Add(new Notification
            {
                UserId = adminId,
                Title = title,
                Body = body,
                Type = "support",
            });
        }
    }

    private async Task<string> ResolveUserLabelAsync(Guid userId, CancellationToken ct)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.FullName, u.Phone })
            .FirstOrDefaultAsync(ct);

        if (user is null) return userId.ToString();
        if (!string.IsNullOrWhiteSpace(user.FullName)) return user.FullName!;
        return user.Phone;
    }

    private TripListItemDto MapTripListItemWithoutBooking(Trip trip)
    {
        var now = _clock.UtcNow;
        var status = ResolveStatus(trip, now);
        var vehicle = trip.Driver?.Vehicle;

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
            null,
            null,
            null);
    }

    private TripListItemDto MapTripListItem(Trip trip, Booking booking)
    {
        var now = _clock.UtcNow;
        var status = ResolveStatus(trip, now);
        var vehicle = trip.Driver?.Vehicle;

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
            null,
            null,
            null);
    }

    private static string ResolveStatus(Trip trip, DateTime now)
    {
        if (trip.Status == TripStatus.InProgress) return "current";
        if (trip.Status == TripStatus.Completed) return "completed";
        if (trip.Status == TripStatus.Cancelled) return "cancelled";
        if (trip.ScheduledAt <= now) return "completed";
        return "upcoming";
    }

    private Guid RequireUserId() =>
        _currentUser.UserId ?? throw new UnauthorizedAppException("غير مصرح");

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
