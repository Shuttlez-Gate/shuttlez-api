using Microsoft.AspNetCore.SignalR;
using Shuttlez.API.Hubs;
using Shuttlez.Application.Common.Interfaces;

namespace Shuttlez.API.Services;

public class DriverRealtimeNotifier : IDriverRealtimeNotifier
{
    private readonly IHubContext<DriverHub> _hub;

    public DriverRealtimeNotifier(IHubContext<DriverHub> hub) => _hub = hub;

    public Task NotifyTripsChangedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _hub.Clients
            .Group(DriverHub.GroupName(userId))
            .SendAsync(
                "TripsChanged",
                new { userId, at = DateTime.UtcNow },
                cancellationToken);
    }
}
