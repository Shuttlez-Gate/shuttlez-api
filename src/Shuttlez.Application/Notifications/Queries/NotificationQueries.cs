using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.Notifications.DTOs;

namespace Shuttlez.Application.Notifications.Queries;

public record GetMyNotificationsQuery : IRequest<IReadOnlyList<NotificationGroupDto>>;

public record MarkNotificationReadCommand(Guid NotificationId) : IRequest<bool>;

public class NotificationHandlers :
    IRequestHandler<GetMyNotificationsQuery, IReadOnlyList<NotificationGroupDto>>,
    IRequestHandler<MarkNotificationReadCommand, bool>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public NotificationHandlers(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<NotificationGroupDto>> Handle(
        GetMyNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAppException("غير مصرح");

        var items = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsDeleted)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationItemDto(
                n.Id,
                n.Title,
                n.Body,
                n.CreatedAt.ToString("HH:mm"),
                !n.IsRead,
                n.CreatedAt))
            .ToListAsync(cancellationToken);

        return items
            .GroupBy(n => n.CreatedAt.ToString("dd-MM-yyyy"))
            .Select(g => new NotificationGroupDto(g.Key, g.ToList()))
            .ToList();
    }

    public async Task<bool> Handle(
        MarkNotificationReadCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAppException("غير مصرح");

        var notification = await _db.Notifications
            .FirstOrDefaultAsync(
                n => n.Id == request.NotificationId &&
                     n.UserId == userId &&
                     !n.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException("الإشعار غير موجود");

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }
}
