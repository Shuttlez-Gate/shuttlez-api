namespace Shuttlez.Application.Common.Interfaces;

public interface ISupportChatRealtimeNotifier
{
    Task NotifyMessageAsync(
        Guid ticketId,
        Guid messageId,
        bool isFromSupport,
        string content,
        DateTime createdAt,
        CancellationToken cancellationToken = default);
}
