namespace Shuttlez.Application.Notifications.DTOs;

public record NotificationItemDto(
    Guid Id,
    string Title,
    string Body,
    string Time,
    bool IsUnread,
    DateTime CreatedAt);

public record NotificationGroupDto(
    string DateLabel,
    IReadOnlyList<NotificationItemDto> Items);
