using Shuttlez.Application.Common.Interfaces;

namespace Shuttlez.Infrastructure.Services;

public sealed class NullSupportChatRealtimeNotifier : ISupportChatRealtimeNotifier
{
    public Task NotifyMessageAsync(
        Guid ticketId,
        Guid messageId,
        bool isFromSupport,
        string content,
        DateTime createdAt,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
