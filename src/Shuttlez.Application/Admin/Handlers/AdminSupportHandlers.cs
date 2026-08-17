using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Domain.Entities;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Admin.Handlers;

public record AdminTicketsQuery(
    string? Search = null,
    string? Status = null,
    int? Page = null,
    int? PageSize = null) : IRequest<PagedResult<AdminSupportTicketDto>>;

public record AdminTicketMessagesQuery(Guid TicketId)
    : IRequest<IReadOnlyList<AdminSupportMessageDto>>;

public record ReplyTicketCommand(Guid TicketId, ReplyTicketRequest Request)
    : IRequest<AdminSupportMessageDto>;

public record UpdateTicketStatusCommand(Guid TicketId, UpdateTicketStatusRequest Request)
    : IRequest<AdminSupportTicketDto>;

public record AdminNotificationsQuery(
    Guid? UserId = null,
    int? Page = null,
    int? PageSize = null) : IRequest<PagedResult<AdminNotificationDto>>;

public record BroadcastNotificationCommand(BroadcastNotificationRequest Request)
    : IRequest<BroadcastResultDto>;

public class AdminSupportHandlers :
    IRequestHandler<AdminTicketsQuery, PagedResult<AdminSupportTicketDto>>,
    IRequestHandler<AdminTicketMessagesQuery, IReadOnlyList<AdminSupportMessageDto>>,
    IRequestHandler<ReplyTicketCommand, AdminSupportMessageDto>,
    IRequestHandler<UpdateTicketStatusCommand, AdminSupportTicketDto>,
    IRequestHandler<AdminNotificationsQuery, PagedResult<AdminNotificationDto>>,
    IRequestHandler<BroadcastNotificationCommand, BroadcastResultDto>
{
    private static readonly string[] TicketStatuses = ["open", "pending", "closed"];

    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUserService _currentUser;
    private readonly ISupportChatRealtimeNotifier _realtime;

    public AdminSupportHandlers(
        IAppDbContext db,
        IDateTimeProvider clock,
        ICurrentUserService currentUser,
        ISupportChatRealtimeNotifier realtime)
    {
        _db = db;
        _clock = clock;
        _currentUser = currentUser;
        _realtime = realtime;
    }

    public async Task<PagedResult<AdminSupportTicketDto>> Handle(
        AdminTicketsQuery request,
        CancellationToken cancellationToken)
    {
        var page = PageRequest.From(request.Page, request.PageSize);
        var query = _db.SupportTickets.Where(t => !t.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim().ToLowerInvariant();
            query = status == "current"
                ? query.Where(t => t.Status != "closed")
                : query.Where(t => t.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(t => t.Subject.Contains(term) || t.User.Phone.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(TicketProjection())
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminSupportTicketDto>(items, page.Page, page.PageSize, total);
    }

    public async Task<IReadOnlyList<AdminSupportMessageDto>> Handle(
        AdminTicketMessagesQuery request,
        CancellationToken cancellationToken)
    {
        var exists = await _db.SupportTickets
            .AnyAsync(t => t.Id == request.TicketId && !t.IsDeleted, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException("التذكرة غير موجودة");
        }

        return await _db.SupportMessages
            .Where(m => m.TicketId == request.TicketId && !m.IsDeleted)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new AdminSupportMessageDto(
                m.Id, m.TicketId, m.IsFromSupport, m.Content, m.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminSupportMessageDto> Handle(
        ReplyTicketCommand request,
        CancellationToken cancellationToken)
    {
        var content = request.Request.Content?.Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new AppException("نص الرد مطلوب");
        }

        var ticket = await _db.SupportTickets
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("التذكرة غير موجودة");

        var senderId = _currentUser.UserId
            ?? throw new UnauthorizedAppException("يجب تسجيل الدخول كمسؤول");

        var message = new SupportMessage
        {
            TicketId = ticket.Id,
            SenderId = senderId,
            IsFromSupport = true,
            Content = content
        };
        _db.Add(message);

        if (ticket.Status == "closed")
        {
            ticket.Status = "open";
            ticket.ClosedAt = null;
        }
        ticket.UpdatedAt = _clock.UtcNow;

        _db.Add(new Notification
        {
            UserId = ticket.UserId,
            Title = "رد جديد من الدعم",
            Body = content.Length > 120 ? content[..120] + "…" : content,
            Type = "support"
        });

        await _db.SaveChangesAsync(cancellationToken);

        await _realtime.NotifyMessageAsync(
            ticket.Id,
            message.Id,
            isFromSupport: true,
            message.Content,
            message.CreatedAt == default ? _clock.UtcNow : message.CreatedAt,
            cancellationToken);

        return message.ToDto();
    }

    public async Task<AdminSupportTicketDto> Handle(
        UpdateTicketStatusCommand request,
        CancellationToken cancellationToken)
    {
        var status = request.Request.Status?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(status) || !TicketStatuses.Contains(status))
        {
            throw new AppException("حالة غير مسموحة. المسموح: " + string.Join(" / ", TicketStatuses));
        }

        var ticket = await _db.SupportTickets
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("التذكرة غير موجودة");

        ticket.Status = status;
        ticket.ClosedAt = status == "closed" ? _clock.UtcNow : null;
        ticket.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return await _db.SupportTickets
            .Where(t => t.Id == ticket.Id)
            .Select(TicketProjection())
            .FirstAsync(cancellationToken);
    }

    public async Task<PagedResult<AdminNotificationDto>> Handle(
        AdminNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var page = PageRequest.From(request.Page, request.PageSize);
        var query = _db.Notifications.Where(n => !n.IsDeleted);

        if (request.UserId is not null)
        {
            query = query.Where(n => n.UserId == request.UserId);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(n => new AdminNotificationDto(
                n.Id, n.UserId, n.User.Phone, n.Title, n.Body, n.Type, n.IsRead, n.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminNotificationDto>(items, page.Page, page.PageSize, total);
    }

    public async Task<BroadcastResultDto> Handle(
        BroadcastNotificationCommand request,
        CancellationToken cancellationToken)
    {
        var body = request.Request;
        if (string.IsNullOrWhiteSpace(body.Title) || string.IsNullOrWhiteSpace(body.Body))
        {
            throw new AppException("العنوان والنص مطلوبان");
        }

        var audience = body.Audience?.Trim().ToLowerInvariant() ?? "all";
        var query = _db.Users.Where(u => !u.IsDeleted && u.IsActive);

        query = audience switch
        {
            "passengers" => query.Where(u => u.UserType == UserType.Passenger),
            "drivers" => query.Where(u => u.UserType == UserType.Driver),
            "admins" => query.Where(u => u.UserType == UserType.Admin),
            "selected" => query.Where(u => body.UserIds != null && body.UserIds.Contains(u.Id)),
            _ => query
        };

        if (audience == "selected" && (body.UserIds is null || body.UserIds.Count == 0))
        {
            throw new AppException("اختر مستخدماً واحداً على الأقل");
        }

        var userIds = await query.Select(u => u.Id).ToListAsync(cancellationToken);
        if (userIds.Count == 0)
        {
            throw new AppException("لا يوجد مستخدمون مطابقون");
        }

        foreach (var userId in userIds)
        {
            _db.Add(new Notification
            {
                UserId = userId,
                Title = body.Title.Trim(),
                Body = body.Body.Trim(),
                Type = string.IsNullOrWhiteSpace(body.Type) ? "admin" : body.Type.Trim()
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new BroadcastResultDto(userIds.Count);
    }

    private System.Linq.Expressions.Expression<Func<SupportTicket, AdminSupportTicketDto>>
        TicketProjection() =>
        t => new AdminSupportTicketDto(
            t.Id,
            t.UserId,
            t.User.Phone,
            t.User.FullName,
            t.TripId,
            t.Subject,
            t.Status,
            _db.SupportMessages.Count(m => m.TicketId == t.Id && !m.IsDeleted),
            _db.SupportMessages
                .Where(m => m.TicketId == t.Id && !m.IsDeleted)
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => m.Content)
                .FirstOrDefault(),
            _db.SupportMessages
                .Where(m => m.TicketId == t.Id && !m.IsDeleted)
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => (DateTime?)m.CreatedAt)
                .FirstOrDefault(),
            t.ClosedAt,
            t.CreatedAt);
}
