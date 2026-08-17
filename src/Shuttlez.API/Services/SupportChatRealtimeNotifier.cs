using Microsoft.AspNetCore.SignalR;
using Shuttlez.API.Hubs;
using Shuttlez.Application.Common.Interfaces;

namespace Shuttlez.API.Services;

public class SupportChatRealtimeNotifier : ISupportChatRealtimeNotifier
{
    private readonly IHubContext<SupportChatHub> _hub;

    public SupportChatRealtimeNotifier(IHubContext<SupportChatHub> hub) => _hub = hub;

    public Task NotifyMessageAsync(
        Guid ticketId,
        Guid messageId,
        bool isFromSupport,
        string content,
        DateTime createdAt,
        CancellationToken cancellationToken = default)
    {
        return _hub.Clients
            .Group(SupportChatHub.GroupName(ticketId))
            .SendAsync(
                "MessageReceived",
                new
                {
                    id = messageId,
                    ticketId,
                    isFromSupport,
                    content,
                    createdAt,
                    time = createdAt.ToLocalTime().ToString("HH:mm"),
                },
                cancellationToken);
    }
}
