namespace Shuttlez.Application.Common.Interfaces;

public record PushNotificationMessage(
    string Type,
    string Title,
    string Body,
    IReadOnlyDictionary<string, string> Data);

/// <summary>
/// Best-effort FCM delivery. Never throws to callers for delivery failures.
/// </summary>
public interface IPushNotificationService
{
    Task SendToUserAsync(Guid userId, PushNotificationMessage message, CancellationToken cancellationToken = default);
    Task SendToUsersAsync(IEnumerable<Guid> userIds, PushNotificationMessage message, CancellationToken cancellationToken = default);
    Task SendToDeviceAsync(string token, PushNotificationMessage message, CancellationToken cancellationToken = default);
}
