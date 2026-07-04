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

    public SupportHandlers(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
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

        return tickets
            .Select(ticket =>
            {
                bookings.TryGetValue(ticket.TripId ?? Guid.Empty, out var booking);
                return MapTicket(ticket, booking, isCompletedTab);
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
            booking = await _db.Bookings
                .Include(b => b.Trip)
                    .ThenInclude(t => t.Route)
                .Include(b => b.Trip)
                    .ThenInclude(t => t.Driver!)
                        .ThenInclude(d => d.Vehicle)
                .FirstOrDefaultAsync(
                    b => b.UserId == userId && b.TripId == request.Request.TripId.Value,
                    cancellationToken);

            trip = booking?.Trip
                ?? throw new NotFoundException("الرحلة غير موجودة");
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
        await _db.SaveChangesAsync(cancellationToken);

        return new SendSupportMessageResponse(message.Id, "تم إرسال الرسالة");
    }

    private SupportTicketDto MapTicket(
        SupportTicket ticket,
        Booking? booking,
        bool isCompletedTab)
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

        return new SupportTicketDto(
            ticket.Id,
            isCompletedTab ? "completed" : "current",
            isCompletedTab ? "completed" : "active",
            tripDto,
            seatsLabel,
            paymentLabel);
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
